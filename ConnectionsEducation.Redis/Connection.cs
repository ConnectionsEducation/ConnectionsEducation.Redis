using System;
using System.Collections;
using System.Threading;

namespace ConnectionsEducation.Redis {
	public class Connection {
		private const int TIMEOUT = 60000;


		public bool ping() {
			ConnectionClient client = new ConnectionClient(Command.fromString("PING\r\n"));
			Queue result = null;
			ManualResetEventSlim reset = new ManualResetEventSlim(false);

			client.objectReceived += (s, e) => {
				result = (Queue)e.Object;
				reset.Set();
			};
			client.initiate();

			reset.Wait(TIMEOUT);

			string pong = resultToString(result);
			return pong == "PONG";
		}

		public void set(string key, string value) {
			Command command = new Command("SET", key, value);
			ConnectionClient client = new ConnectionClient(command);
			Queue result = null;
			ManualResetEventSlim reset = new ManualResetEventSlim(false);

			client.objectReceived += (s, e) => {
				result = (Queue)e.Object;
				reset.Set();
			};
			client.initiate();

			reset.Wait(TIMEOUT);

			string ok = resultToString(result);
			if (ok != "OK")
				throw new InvalidOperationException("Set result is not OK!");
		}

		public string get(string key) {
			Command command = new Command("GET", key);
			ConnectionClient client = new ConnectionClient(command);
			Queue result = null;
			ManualResetEventSlim reset = new ManualResetEventSlim(false);

			client.objectReceived += (s, e) => {
				result = (Queue)e.Object;
				reset.Set();
			};
			client.initiate();

			reset.Wait(TIMEOUT);

			string value = resultToString(result);
			return value;
		}

		private static string resultToString(Queue result) {
			if (result == null || result.Count == 0)
				return null;
			object data = result.Dequeue();
			if (data == null)
				return null;
			return data.ToString();
		}
	}
}