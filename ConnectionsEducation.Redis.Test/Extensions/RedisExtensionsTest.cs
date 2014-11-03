using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Extensions.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class RedisExtensionsTest {

		/// <summary>
		/// Per-test initialization: clear the database
		/// </summary>
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

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testAllKeys_returnsAllKeysOnce() {
			using (Redis redis = new Redis()) {
				string[] keys = {
					"key1", "key2", "key3", "key4", "test1", "test2", "test3", "test4",
					"key5", "key6", "key7", "key8", "test5", "test6", "test7", "test8",
					"foo1", "foo2", "foo3", "foo4", "fuzz1", "fuzz2", "fuzz3", "fuzz4",
					"foo5", "foo6", "foo7", "foo8", "fuzz5", "fuzz6", "fuzz7", "fuzz8",
				};
				foreach (string key in keys) {
					redis.set(key, "scantest");
				}

				List<string> keysFound = new List<string>();
				foreach (string key in redis.allKeys()) {
					keysFound.Add(key);
				}

				// Set equality when A < B and B < A
				CollectionAssert.IsSubsetOf(keysFound, keys);
				CollectionAssert.IsSubsetOf(keys, keysFound);
				// SCAN can return keys multiple times
				CollectionAssert.AllItemsAreUnique(keysFound);
			}
		}
	}
}
