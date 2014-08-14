using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Extensions.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class RedisExtensionsTest {
		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testGetSetBytes() {
			using (Redis redis = new Redis()) {
				byte[] expected = Guid.NewGuid().ToByteArray();
				redis.setBytes("foobytes", expected);
				byte[] actual = redis.getBytes("foobytes");
				CollectionAssert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHGetAll() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hmset("foohash", "foo1", "bar1", "foo2", "bar2", "foo3", "bar3");
				var result = redis.hgetall("foohash");
				Assert.AreEqual("bar1", result["foo1"]);
				Assert.AreEqual("bar2", result["foo2"]);
				Assert.AreEqual("bar3", result["foo3"]);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testExpireAt_oldExpiryDeletesKey() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "bar");
				redis.expireat("foo", DateTime.Now.AddSeconds(-1));
				Assert.IsFalse(redis.exists("foo"));
				redis.set("foo", "bar");
				redis.expireat("foo", DateTime.Now.AddSeconds(5));
				Assert.AreEqual(5, redis.numberCommand(new Command("TTL", "foo")), "TTL not 5 seconds after expiring now + 5 seconds. Check latency before assuming test failure.");
			}
		}
	}
}
