using System;
using System.Diagnostics;

namespace ConsoleMemoryTest {
	class Program {
		static void Main(string[] args) {
			Stopwatch stopwatch = new Stopwatch();
			const int ITERS = 100;
			const int BATCH = 1000;
			double lastMs = 0.0;

			stopwatch.Start();
			for (int iter = 1; iter <= ITERS; ++iter) {
				int success = 0;
				using (ConnectionsEducation.Redis.Connection conn = new ConnectionsEducation.Redis.Connection()) {
					for (int i = 0; i < BATCH; ++i) {
						string guid = Guid.NewGuid().ToString("d");
						conn.set("foo" + i, guid);
						string value = conn.get("foo" + i);
						if (guid == value)
							success++;
					}
				}

				Console.Write("[{0:mm\\:ss\\.f}]", stopwatch.Elapsed);
				Console.Write(success == BATCH ? "+  " : "-  ");
				if (lastMs > 0.01)
					Console.Write("{0:0.00}ops/s, ", 1000.0 * BATCH / (stopwatch.ElapsedMilliseconds - lastMs));
				lastMs = stopwatch.ElapsedMilliseconds;
				double rateAvg = 1000.0 * iter * BATCH / lastMs;
				Console.WriteLine("Avg: {0:0.00}ops/s", rateAvg);
			}
			stopwatch.Stop();

			Console.WriteLine();
			Console.Write("[{0:mm\\:ss\\.f}]", stopwatch.Elapsed);
			Console.WriteLine("   Total Avg: {0:0.00}ops/s", 1000.0 * ITERS * BATCH / lastMs);
			Console.WriteLine("Done.");
			Console.ReadLine();
		}
	}
}
