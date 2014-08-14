using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ConnectionsEducation.Redis;

namespace ConsoleMemoryTest {
	/// <summary>
	/// Runs a series of GET/SET operations against a connected Redis server.
	/// </summary>
	class Program {
		/// <summary>
		/// Defines the entry point of the application.
		/// </summary>
		/// <param name="args">The arguments.</param>
		static void Main(string[] args) {
			Stopwatch stopwatch = new Stopwatch();
			const int ITERS = 100;
			const int BATCH = 1000;
			const int OPS_PER_ITERATION = 3 * BATCH + 2;
			double lastMs = 0.0;
			const bool PIPELINE = false;

			stopwatch.Start();
			for (int iter = 1; iter <= ITERS; ++iter) {
				int success = 0;
				string bar;
				string host = args.Length > 0 ? args[0] : "127.0.0.1";
				int port = args.Length > 1 ? Convert.ToInt32(args[1]) : 6379;
// ReSharper disable once RedundantAssignment
				using (Redis conn = new Redis(host, port)) {
// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if (PIPELINE) {
						StringBuilder commandBuilder = new StringBuilder();
						Func<string, string> bulkString = s => string.Format("${0}\r\n{1}\r\n", s.Length, s);
						commandBuilder.AppendFormat("*3\r\n{0}{1}{2}", bulkString("SET"), bulkString("bar"), bulkString("0"));
						List<string> guids = new List<string>();
						for (int i = 0; i < BATCH; ++i) {
							string guid = Guid.NewGuid().ToString("d");
							guids.Add(guid);
							commandBuilder.AppendFormat("*3\r\n{0}{1}{2}", bulkString("SET"), bulkString("foo" + i), bulkString(guid));
							commandBuilder.AppendFormat("*2\r\n{0}{1}", bulkString("GET"), bulkString("foo" + i));
							commandBuilder.AppendFormat("*2\r\n{0}{1}", bulkString("INCR"), bulkString("bar"));
						}
						commandBuilder.AppendFormat("*2\r\n{0}{1}", bulkString("GET"), bulkString("bar"));
						Command command = Command.fromString(commandBuilder.ToString());
						var results = conn.enumerateCommand(command);
						results.MoveNext(); // set bar 0
						foreach (string guid in guids) {
							results.MoveNext(); // set foo0 guuuiiiiddd
							results.MoveNext(); // get foo0
							string value = Encoding.ASCII.GetString((byte[])results.Current);
							if (guid == value)
								success++;
							results.MoveNext(); // incr bar
						}
						results.MoveNext(); // get bar;
						bar = Encoding.ASCII.GetString((byte[])results.Current);
						results.MoveNext(); // --false
					} else {
						conn.set("bar", "0");
						for (int i = 0; i < BATCH; ++i) {
							string guid = Guid.NewGuid().ToString("d");
							conn.set("foo" + i, guid);
							string value = conn.get("foo" + i);
							conn.incr("bar");
							if (guid == value)
								success++;
						}
						bar = conn.get("bar");
					}
				}

				Console.Write("[{0:mm\\:ss\\.f}]", stopwatch.Elapsed);
				Console.Write(success == BATCH ? "+  " : "-  ");
				if (lastMs > 0.01)
					Console.Write("{0:0.00}ops/s, ", 1.0 * OPS_PER_ITERATION  * BATCH / (stopwatch.ElapsedMilliseconds - lastMs));
				lastMs = stopwatch.ElapsedMilliseconds;
				double rateAvg = 1.0 * OPS_PER_ITERATION * iter * BATCH / lastMs;
				Console.WriteLine("Avg: {0:0.00}ops/s    (bar={1})", rateAvg, bar);
			}
			stopwatch.Stop();

			Console.WriteLine();
			Console.Write("[{0:mm\\:ss\\.f}]", stopwatch.Elapsed);
			Console.WriteLine("   Total Avg: {0:0.00}ops/s", 1.0 * OPS_PER_ITERATION  * ITERS * BATCH / lastMs);
			Console.WriteLine("Done.");
			Console.ReadLine();
		}
	}
}
