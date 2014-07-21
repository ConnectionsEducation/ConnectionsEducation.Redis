using System;
using System.Diagnostics;

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

			stopwatch.Start();
			for (int iter = 1; iter <= ITERS; ++iter) {
				int success = 0;
				string bar;
				string host = args.Length > 0 ? args[0] : "127.0.0.1";
				using (ConnectionsEducation.Redis.Redis conn = new ConnectionsEducation.Redis.Redis(host)) {
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
