using System;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// Represents a Redis error (a minus on the wire protocol)
	/// </summary>
	public class RedisErrorException : Exception {
		/// <summary>
		/// Creates an instance of the RedisErrorException class
		/// </summary>
		/// <param name="errorMessage">The error message from Redis</param>
		public RedisErrorException(string errorMessage) : base(errorMessage) {
		}
	}
}