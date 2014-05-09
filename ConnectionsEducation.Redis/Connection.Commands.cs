using System;

namespace ConnectionsEducation.Redis {
	public partial class Connection {

		public bool ping() {
			Command command = Command.fromString("PING\r\n");
			string pong = resultToString(sendCommand(command));
			return pong == "PONG";
		}

		public void set(string key, string value) {
			Command command = new Command("SET", key, value);
			string ok = resultToString(sendCommand(command));
			if (ok != "OK")
				throw new InvalidOperationException("Set result is not OK!");
		}

		public string get(string key) {
			Command command = new Command("GET", key);
			string value = resultToString(sendCommand(command));
			return value;
		}

		public long del(params string[] keys) {
			Command command = new Command("DEL", keys);
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		public long incr(string key) {
			Command command = new Command("INCR", key);
			long value = resultToNumber(sendCommand(command));
			return value;
		}
	}
}
