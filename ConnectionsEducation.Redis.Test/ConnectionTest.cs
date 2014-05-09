using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class ConnectionTest {
		[TestMethod]
		public void testPing() {
			using (Connection connection = new Connection()) {
				Assert.IsTrue(connection.ping());
			}
		}

		[TestMethod]
		public void testSetGetString() {
			using (Connection connection = new Connection()) {
				connection.set("foo", "bar");
				string actual = connection.get("foo");
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
			using (Connection connection = new Connection()) {
				connection.set("foo", expected);
				string actual = connection.get("foo");
				Assert.AreEqual(expected, actual);
			}
		}

		[TestMethod]
		public void testDel() {
			using (Connection connection = new Connection()) {
				connection.set("foo", "1");
				connection.set("bar", "2");
				Assert.AreEqual(2L, connection.del("foo", "bar", "baz"));
			}
		}

		[TestMethod]
		public void testIncr() {
			using (Connection connection = new Connection()) {
				connection.set("foo", "0");
				connection.set("food", "0");
				connection.set("bar", "0");
				connection.incr("foo");
				connection.incr("foo");
				connection.incr("foo");
				connection.incr("bar");
				connection.incr("food");
				Assert.AreEqual(4L, connection.incr("foo"));
			}
		}

		[TestMethod]
		public void testZAddZRem() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				Assert.AreEqual(0L, connection.zrem("foo", "barnone", "baz"));
				Assert.AreEqual(1L, connection.zrem("foo", "bar1"));
				Assert.AreEqual(0L, connection.zrem("foo", "bar1"));
			}
		}

		[TestMethod]
		public void testZCard() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				Assert.AreEqual(2L, connection.zcard("foo"));
			}
		}

		[TestMethod]
		public void testZRange_inOrder() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(1L, "bar1"), Tuple.Create(2L, "bar2"));
				string[] range = connection.zrange("foo", 0);
				Assert.AreEqual(2, range.Length);
				Assert.AreEqual("bar1", range[0]);
				Assert.AreEqual("bar2", range[1]);
			}
		}

		[TestMethod]
		public void testZRange_outOfOrder() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				string[] range = connection.zrange("foo", 0);
				Assert.AreEqual(2, range.Length);
				Assert.AreEqual("bar1", range[0]);
				Assert.AreEqual("bar2", range[1]);
			}
		}

		[TestMethod]
		public void testZScore_value() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				double? value = connection.zscore("foo", "bar2");
				Assert.IsNotNull(value);
				Assert.AreEqual(2, value.Value, 0.000001);
			}
		}

		[TestMethod]
		public void testZScore_null() {
			using (Connection connection = new Connection()) {
				connection.del("foo");
				connection.zadd("foo", Tuple.Create(2L, "bar2"), Tuple.Create(1L, "bar1"));
				double? value = connection.zscore("foo", "bar3");
				Assert.IsNull(value);
			}
		}
	}
}
