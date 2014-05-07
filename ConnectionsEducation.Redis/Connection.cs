using System;
using System.Collections;
using System.Threading;

namespace ConnectionsEducation.Redis {
	public class Connection {
		private const int TIMEOUT = 60000;


		public bool ping() {
			ConnectionClient client = new ConnectionClient("PING\r\n");
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
			int keyLength = key.Length;
			int valueLength = value.Length;
			string command = string.Format("*3\r\n$3\r\nSET\r\n${0}\r\n{1}\r\n${2}\r\n{3}\r\n", keyLength, key, valueLength, value);
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
			int keyLength = key.Length;
			string command = string.Format("*2\r\n$3\r\nGET\r\n${0}\r\n{1}\r\n", keyLength, key);
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