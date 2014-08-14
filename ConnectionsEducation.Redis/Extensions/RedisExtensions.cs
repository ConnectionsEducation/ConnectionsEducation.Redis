using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectionsEducation.Redis.Extensions {
	/// <summary>
	/// Extension methods to overload base functionality.
	/// </summary>
	public static class RedisExtensions {
		/// <summary>
		/// Get the value for a key as a byte array.
		/// </summary>
		/// <param name="redis">The Redis client</param>
		/// <param name="key">The key</param>
		/// <returns>The value</returns>
		public static byte[] getBytes(this Redis redis, string key) {
			Command command = new Command("GET", key);
			return (byte[])redis.executeCommand(command);
		}

		/// <summary>
		/// Set the value for a key as a byte array.
		/// </summary>
		/// <param name="redis">The Redis client</param>
		/// <param name="key">The key</param>
		/// <param name="value">The value</param>
		public static void setBytes(this Redis redis, string key, byte[] value) {
			Command command = new Command("SET", redis.encoding.GetBytes(key), value);
			string ok = redis.stringCommand(command);
			if (ok != "OK")
				throw new InvalidOperationException("SET result is not OK!");
		}

		/// <summary>
		/// Gets all of the keys and values for the specified hash
		/// </summary>
		/// <param name="redis">The Redis client</param>
		/// <param name="key">The key</param>
		/// <returns>The fields and their values</returns>
		public static IDictionary<string, string> hgetall(this Redis redis, string key) {
			Command command = new Command("HGETALL", key);
			var fieldsAndValues = redis.arrayCommand(command).Select(data => redis.encoding.GetString((byte[])data)).ToList();
			return fieldsAndValues.Where((o, i) => i % 2 == 0).Select(Convert.ToString)
				.Zip(fieldsAndValues.Where((o, i) => i % 2 != 0).Select(Convert.ToString), (even, odd) => new {field = even, value = odd})
				.ToDictionary(fieldValue => fieldValue.field, fieldValue => fieldValue.value);
		}

		/// <summary>
		/// Sets the expiry time of the specified key to the given value
		/// </summary>
		/// <param name="redis">The redis client</param>
		/// <param name="key">The key</param>
		/// <param name="timeToExpire">The time to expire the key</param>
		/// <returns>True if the expiry was set; false if the expiry could not be set.</returns>
		public static bool expireat(this Redis redis, string key, DateTime timeToExpire) {
			DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime utc = timeToExpire.ToUniversalTime();
			TimeSpan span = utc.Date.Subtract(epoch);
			string unixTime = (span.TotalDays * 86400 + utc.TimeOfDay.TotalSeconds).ToString("0");
			Command command = new Command("EXPIREAT", key, unixTime);
			long updatedExpiry = redis.numberCommand(command);
			return updatedExpiry > 0;
		}
	}
}
