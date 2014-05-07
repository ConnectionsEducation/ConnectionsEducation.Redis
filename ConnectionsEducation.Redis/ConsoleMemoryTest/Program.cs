using System;

namespace ConsoleMemoryTest {
	class Program {
		static void Main(string[] args) {
			DateTime begin = DateTime.Now;

			while (DateTime.Now < begin.AddMinutes(5)) {
				int success = 0;
				for (int i = 0; i < 1000; ++i) {
					string guid = Guid.NewGuid().ToString("d");
					var conn = new ConnectionsEducation.Redis.Connection();
					conn.set("foo" + i, guid);
					string value = conn.get("foo" + i);
					if (guid == value)
						success++;
				}

				Console.WriteLine("{0}: {1}", DateTime.Now.Subtract(begin).TotalSeconds, success);
			}

			Console.WriteLine();
			Console.WriteLine("Done.");
			Console.ReadLine();
		}
	}
}
