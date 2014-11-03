using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
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

		[TestInitialize]
		public void testInitialize() {
			using (Redis redis = new Redis()) {
				redis.executeCommand(new Command("flushall", new string[] { }));
			}
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

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testStringCommand() {
			using (Redis redis = new Redis()) {
				const string EXPECTED = "Hello World!";
				Command command = new Command("ECHO", EXPECTED);
				string actual = redis.stringCommand(command);
				Assert.AreEqual(EXPECTED, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testNumberCommand() {
			using (Redis redis = new Redis()) {
				redis.set("foonumber", "41");
				Command command = new Command("INCR", "foonumber");
				long actual = redis.numberCommand(command);
				Assert.AreEqual(42, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testSetBytesGet() {
			using (Redis redis = new Redis()) {
				byte[] expected = {8, 6, 7, 5, 3, 0, 9};
				byte[] key = Encoding.ASCII.GetBytes("foobytes");
				Command command = new Command("SET", key, expected);
				redis.executeCommand(command);
				command = new Command("GET", key);
				byte[] actual = redis.executeCommand(command) as byte[];
				CollectionAssert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testDoubleCommand() {
			using (Redis redis = new Redis()) {
				Command command = Command.fromString("*1\r\n$4\r\nPING\r\n*1\r\n$4\r\nPING\r\n");
				IEnumerator enumerator = redis.enumerateCommand(command);
				Assert.IsTrue(enumerator.MoveNext());
				Assert.AreEqual("PONG", Encoding.ASCII.GetString((byte[])enumerator.Current));
				Assert.IsTrue(enumerator.MoveNext());
				Assert.AreEqual("PONG", Encoding.ASCII.GetString((byte[])enumerator.Current));
				Assert.IsFalse(enumerator.MoveNext());
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHIncrBy_existing() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "value", "41");
				long actual = redis.hincrby("foohash", "value", 1);
				Assert.AreEqual(42L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHIncrBy_new() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				long actual = redis.hincrby("foohash", "value", 1L);
				Assert.AreEqual(1L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHIncrBy_newhash() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				long actual = redis.hincrby("foohash", "value", 42L);
				Assert.AreEqual(42L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHIncrBy_negative() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "value", "3");
				long actual = redis.hincrby("foohash", "value", -7);
				Assert.AreEqual(-4L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHKeys() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				redis.hset("foohash", "bar2", "2");
				string[] actual = redis.hkeys("foohash");
				CollectionAssert.Contains(actual, "bar1");
				CollectionAssert.Contains(actual, "bar2");
				CollectionAssert.Contains(actual, "bar3");
				Assert.AreEqual(3, actual.Length);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHExists() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				Assert.IsTrue(redis.hexists("foohash", "bar1"));
				Assert.IsFalse(redis.hexists("foohash", "bar2"));
				Assert.IsTrue(redis.hexists("foohash", "bar3"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHDel() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				long actual = redis.hdel("foohash", "bar1");
				Assert.AreEqual(1L, actual);
				actual = redis.hdel("foohash", "bar1");
				Assert.AreEqual(0L, actual);
				actual = redis.hdel("foohash", "bar2");
				Assert.AreEqual(0L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHDel_hashDoesNotExist() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				long actual = redis.hdel("foohash", "bar1");
				Assert.AreEqual(0L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHDel_multipleFields() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				long actual = redis.hdel("foohash", "bar1", "bar2", "bar3");
				Assert.AreEqual(2L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHLen() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				long actual = redis.hlen("foohash");
				Assert.AreEqual(2L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHLen_notExists() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				long actual = redis.hlen("foohash");
				Assert.AreEqual(0L, actual);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHSetNx() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "value", "bar");
				Assert.IsFalse(redis.hsetnx("foohash", "value", "baz"));
				Assert.IsTrue(redis.hsetnx("foohash", "value2", "2"));
				Assert.AreEqual("bar", redis.hget("foohash", "value"));
				Assert.AreEqual("2", redis.hget("foohash", "value2"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testHVals() {
			using (Redis redis = new Redis()) {
				redis.del("foohash");
				redis.hset("foohash", "bar1", "1");
				redis.hset("foohash", "bar3", "3");
				string[] actual = redis.hvals("foohash");
				CollectionAssert.Contains(actual, "1");
				CollectionAssert.Contains(actual, "3");
				Assert.AreEqual(2, actual.Length);
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof(RedisErrorException))]
		public void testErrorException() {
			using (Redis redis = new Redis()) {
				Command command = new Command("notacommand", "foobar");
				redis.executeCommand(command);
			}
		}

		[TestMethod]
		public void testExists() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "bar");
				Assert.IsTrue(redis.exists("foo"));
				redis.del("foo");
				Assert.IsFalse(redis.exists("foo"));
			}
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testKeysAll() {
			using (Redis redis = new Redis()) {
				redis.set("foo", "bar");
				redis.set("bar", "foo");
				redis.set("baz", "hello world");
				redis.hset("fuzz", "a", "apple");
				redis.hset("fuzz", "b", "banana");

				string[] keys = redis.keys();

				CollectionAssert.Contains(keys, "foo");
				CollectionAssert.Contains(keys, "bar");
				CollectionAssert.Contains(keys, "baz");
				CollectionAssert.Contains(keys, "fuzz");
				Assert.AreEqual(4, keys.Length);
			}
		}
	}
}
