using System;

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
	}
}
