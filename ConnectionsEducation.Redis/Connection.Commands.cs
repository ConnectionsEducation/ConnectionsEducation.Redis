using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

		public long zadd(string setKey, params Tuple<long, string>[] elements) {
			List<string> args = new List<string>();
			args.Add(setKey);
			foreach (Tuple<long, string> element in elements) {
				args.Add(element.Item1.ToString(CultureInfo.InvariantCulture));
				args.Add(element.Item2);
			}
			Command command = new Command("ZADD", args.ToArray());
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		public long zrem(string setKey, params string[] elements) {
			List<string> args = new List<string>();
			args.Add(setKey);
			args.AddRange(elements);
			Command command = new Command("ZREM", args.ToArray());
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		public long zcard(string setKey) {
			Command command = new Command("ZCARD", setKey);
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		public string[] zrange(string setKey, long start, long end = -1) {
			Command command = new Command("ZRANGE", setKey, start.ToString(CultureInfo.InvariantCulture), end.ToString(CultureInfo.InvariantCulture));
			object[] value = sendCommand(command).Dequeue() as object[];
			if (value == null)
				return null;
			return value.Select(element => element.ToString()).ToArray();
		}

		public double? zscore(string setKey, string element) {
			Command command = new Command("ZSCORE", setKey, element);
			string value = resultToString(sendCommand(command));
			double result;
			return double.TryParse(value, out result) ? result : (double?)null;
		}
	}
}
