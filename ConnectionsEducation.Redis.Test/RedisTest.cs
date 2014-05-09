using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class RedisTest {
		[TestMethod]
		public void testPing() {
			using (Redis redis = new Redis()) {
				Assert.IsTrue(redis.ping());
			}
		}

		[TestMethod]
		public void testSetGetString() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "bar");
				string actual = redis.get("foo");
				Assert.AreEqual("bar", actual);
			}
		}

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

		[TestMethod]
		public void testDel() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "1");
				redis.set("bar", "2");
				Assert.AreEqual(2L, redis.del("foo", "bar", "baz"));
				Assert.IsNull(redis.get("foo"));
			}
		}

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

		[TestMethod]
		public void testZCard() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				Assert.AreEqual(2L, redis.zcard("foo"));
			}
		}

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

		[TestMethod]
		public void testZScore_null() {
			using (Redis redis = new Redis()) {
				redis.del("foo");
				redis.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				double? value = redis.zscore("foo", "bar3");
				Assert.IsNull(value);
			}
		}
	}
}
