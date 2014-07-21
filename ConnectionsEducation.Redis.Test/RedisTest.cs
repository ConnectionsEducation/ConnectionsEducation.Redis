using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class RedisTest {
		/// <summary>
		/// The redis-server.exe process
		/// </summary>
		private static Process _redisServer;

		/// <summary>
		/// Start redis-server.exe
		/// </summary>
		/// <param name="testContext">The test context</param>
		[AssemblyInitialize]
		[DeploymentItem("redis-2.6\\redis-server.exe", "redis-2.6")]
		public static void initialize(TestContext testContext) {
			_redisServer = Process.Start("redis-2.6\\redis-server.exe");
		}

		/// <summary>
		/// Stop redis-server.exe
		/// </summary>
		[AssemblyCleanup]
		public static void cleanup() {
			_redisServer.CloseMainWindow();
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testPing() {
			using (Redis redis = new Redis()) {
				Assert.IsTrue(redis.ping());
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testSetGetString() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "bar");
				string actual = redis.get("foo");
				Assert.AreEqual("bar", actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHashSetGetStringMultiple() {
			using (Redis redis = new Redis()) {
				redis.hset("foo-hash", "key1", "bar");
				redis.hset("foo-hash", "key2", "baz");
				string actual = redis.hget("foo-hash", "key2");
				Assert.AreEqual("baz", actual);
				actual = redis.hget("foo-hash", "key1");
				Assert.AreEqual("bar", actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHashSetMGetString() {
			using (Redis redis = new Redis()) {
				redis.hset("foo-hash", "key1", "bar");
				redis.hset("foo-hash", "key2", "baz");
				string[] actual = redis.hmget("foo-hash", "key1", "key2");
				Assert.AreEqual("bar", actual[0]);
				Assert.AreEqual("baz", actual[1]);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHashMGetNotExists() {
			using (Redis redis = new Redis()) {
				redis.hset("foo-hash", "key1", "bar");
				string[] actual = redis.hmget("foo-hash", "key1", "notexist");
				Assert.AreEqual("bar", actual[0]);
				Assert.IsNull(actual[1]);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHashMSetGetStringMultiple() {
			using (Redis redis = new Redis()) {
				redis.hmset("foo-hash", "key1", "bar", "key2", "baz");
				string actual = redis.hget("foo-hash", "key2");
				Assert.AreEqual("baz", actual);
				actual = redis.hget("foo-hash", "key1");
				Assert.AreEqual("bar", actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testSetGetString_largerThanBuffer() {
			string expected = "";
			for (int i = 0; i < ConnectionState.BUFFER_SIZE * 2; ++i) {
				expected += ".";
			}
			expected += "XXX";
			using (Redis redis = new Redis()) {
				redis.set("foo", expected);
				string actual = redis.get("foo");
				Assert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testDel() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "1");
				redis.set("bar", "2");
				Assert.AreEqual(2L, redis.del("foo", "bar", "baz"));
				Assert.IsNull(redis.get("foo"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testIncr() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "0");
				redis.set("food", "0");
				redis.set("bar", "0");
				redis.incr("foo");
				redis.incr("foo");
				redis.incr("foo");
				redis.incr("bar");
				redis.incr("food");
				Assert.AreEqual(4L, redis.incr("foo"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZAddZRem() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				Assert.AreEqual(0L, redis.zrem("foo", "barnone", "baz"));
				Assert.AreEqual(1L, redis.zrem("foo", "bar1"));
				Assert.AreEqual(0L, redis.zrem("foo", "bar1"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZCard() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				Assert.AreEqual(2L, redis.zcard("foo"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZRange_inOrder() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				string[] range = redis.zrange("foo", 0);
				Assert.AreEqual(2, range.Length);
				Assert.AreEqual("bar1", range[0]);
				Assert.AreEqual("bar2", range[1]);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZRange_outOfOrder() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				string[] range = redis.zrange("foo", 0);
				Assert.AreEqual(2, range.Length);
				Assert.AreEqual("bar1", range[0]);
				Assert.AreEqual("bar2", range[1]);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZScore_value() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				double? value = redis.zscore("foo", "bar2");
				Assert.IsNotNull(value);
				Assert.AreEqual(2, value.Value, 0.000001);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testZScore_null() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				double? value = redis.zscore("foo", "bar3");
				Assert.IsNull(value);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testExpire() {
			using (Redis redis = new Redis()) {
				redis.del("foo", "bar");
				redis.set("foo", "bar");
				Assert.IsTrue(redis.expire("foo", 10));
				Assert.IsFalse(redis.expire("bar", 10));
			}
		}
	}
}
