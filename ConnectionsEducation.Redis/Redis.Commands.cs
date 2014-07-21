using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// A high-level application interface for Redis functionality.
	/// </summary>
	public partial class Redis {
		/// <summary>
		/// "Pings" the connected server for connectivity and responsiveness.
		/// </summary>
		/// <returns>True if PING returned PONG; otherwise false.</returns>
		public bool ping() {
			Command command = Command.fromString("PING\r\n");
			string pong = resultToString(sendCommand(command));
			return pong == "PONG";
		}

		/// <summary>
		/// Sets the specified key to the specified string
		/// </summary>
		/// <param name="key">The key to set</param>
		/// <param name="value">The value to set the key to</param>
		public void set(string key, string value) {
			Command command = new Command("SET", key, value);
			string ok = resultToString(sendCommand(command));
			if (ok != "OK")
				throw new InvalidOperationException("SET result is not OK!");
		}

		/// <summary>
		/// Gets the specified key
		/// </summary>
		/// <param name="key">The key to get</param>
		/// <returns>The string stored by the specified key</returns>
		public string get(string key) {
			Command command = new Command("GET", key);
			string value = resultToString(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Deletes the specified keys
		/// </summary>
		/// <param name="keys">The keys to delete</param>
		/// <returns>The number of keys deleted</returns>
		public long del(params string[] keys) {
			Command command = new Command("DEL", keys);
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Increments the specified key by 1
		/// </summary>
		/// <param name="key">The key to increment by 1</param>
		/// <returns>The newly incremented value of the specified key</returns>
		public long incr(string key) {
			Command command = new Command("INCR", key);
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Sets the specified key to expire after expiration has elapsed
		/// </summary>
		/// <param name="key">The key to expire</param>
		/// <param name="seconds">The amount of time to elapse before the key is expired</param>
		/// <returns>True if the expiration was set, false if the key does not exist or the timeout could not otherwise be set</returns>
		public bool expire(string key, int seconds) {
			Command command = new Command("EXPIRE", key, seconds.ToString(CultureInfo.InvariantCulture));
			bool success = resultToNumber(sendCommand(command)) > 0;
			return success;
		}

		/// <summary>
		/// Adds the set of keys (with ordering) to the sorted set
		/// </summary>
		/// <param name="setKey">The key of the sorted set to add elements to</param>
		/// <param name="elements">The set of keys (with ordering) to add to the sorted set</param>
		/// <returns>The number of keys added to the sorted set</returns>
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

		/// <summary>
		/// Removes the elements from the set
		/// </summary>
		/// <param name="setKey">The key of the sorted set to remove the elements from</param>
		/// <param name="elements">The elements to remove from the sorted set</param>
		/// <returns>The number of elements removed</returns>
		public long zrem(string setKey, params string[] elements) {
			List<string> args = new List<string>();
			args.Add(setKey);
			args.AddRange(elements);
			Command command = new Command("ZREM", args.ToArray());
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Returns the number of elements in the sorted set
		/// </summary>
		/// <param name="setKey">The key of the sorted set</param>
		/// <returns>The number of elements in the sorted set</returns>
		public long zcard(string setKey) {
			Command command = new Command("ZCARD", setKey);
			long value = resultToNumber(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Obtains the specified range of elements in the sorted set specified by key
		/// </summary>
		/// <param name="setKey">The key of the sorted set to query</param>
		/// <param name="start">The key to start the range from, 0 is the first element, -1 is the last element</param>
		/// <param name="end">The key to stop the range at (inclusive), 0 is the first element, -1 is the last element</param>
		/// <returns>The range of keys in the sorted set between start and end, in order by cardinality</returns>
		public string[] zrange(string setKey, long start, long end = -1) {
			Command command = new Command("ZRANGE", setKey, start.ToString(CultureInfo.InvariantCulture), end.ToString(CultureInfo.InvariantCulture));
			object[] value = sendCommand(command).Dequeue() as object[];
			if (value == null)
				return null;
			return value.Select(element => element.ToString()).ToArray();
		}

		/// <summary>
		/// Gets the score of the specified element in the specified sorted set.
		/// Score is the value given to the element when adding it to the set
		/// </summary>
		/// <param name="setKey">The sorted set to obtain the element's score from</param>
		/// <param name="element">The element in the sorted set to obtain the score of</param>
		/// <returns>The score of the specified element in the specified set, or null if either the element or set do not exist</returns>
		public double? zscore(string setKey, string element) {
			Command command = new Command("ZSCORE", setKey, element);
			string value = resultToString(sendCommand(command));
			double result;
			return double.TryParse(value, out result) ? result : (double?)null;
		}

		/// <summary>
		/// Sets the value of a field in a hash
		/// </summary>
		/// <param name="key">The object key</param>
		/// <param name="field">The field in the hash</param>
		/// <param name="value">The value</param>
		/// <returns>True if the field is new; otherwise false if the field was updated.</returns>
		public bool hset(string key, string field, string value) {
			Command command = new Command("HSET", key, field, value);
			long isNewValue = resultToNumber(sendCommand(command));
			return isNewValue > 0;
		}

		/// <summary>
		/// Gets the value of a field in a hash
		/// </summary>
		/// <param name="key">The object key</param>
		/// <param name="field">The field in the hash</param>
		/// <returns>The value</returns>
		public string hget(string key, string field) {
			Command command = new Command("HGET", key, field);
			string value = resultToString(sendCommand(command));
			return value;
		}

		/// <summary>
		/// Get multiple fields from a single hash
		/// </summary>
		/// <param name="key">The object key</param>
		/// <param name="fields">The fields in the hash</param>
		/// <returns>The values corresponding to the fields</returns>
		public string[] hmget(string key, params string[] fields) {
			string[] arguments = new string[fields.Length + 1];
			arguments[0] = key;
			Array.Copy(fields, 0, arguments, 1, fields.Length);
			Command command = new Command("HMGET", arguments);
			object[] value = resultToArray(sendCommand(command));
			return value.Select(v => v == null ? null : v.ToString()).ToArray();
		}

		/// <summary>
		/// Set multiple fields and their corresponding values for a single hash
		/// </summary>
		/// <param name="key">The object key</param>
		/// <param name="fieldsValues">The fields and values, given in the order field1, value1, field2, value2, ...</param>
		public void hmset(string key, params string[] fieldsValues) {
			string[] arguments = new string[fieldsValues.Length + 1];
			arguments[0] = key;
			Array.Copy(fieldsValues, 0, arguments, 1, fieldsValues.Length);
			Command command = new Command("HMSET", arguments);
			string ok = resultToString(sendCommand(command));
			if (ok != "OK")
				throw new InvalidOperationException("HMSET result is not OK!");
		}
	}
}