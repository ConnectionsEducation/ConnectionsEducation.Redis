using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class AuthTest {
		private static Process _redisServer;

		[ClassInitialize]
		[DeploymentItem("redis-2.8\\redis-auth.conf", "redis-2.8")]
		public static void initialize(TestContext context) {
			_redisServer = Process.Start("redis-2.8\\redis-server.exe", "redis-2.8\\redis-auth.conf");
			Thread.Sleep(500);
		}

		[ClassCleanup]
		public static void cleanup() {
			_redisServer.CloseMainWindow();
			Thread.Sleep(500);
		}

		[TestMethod]
		[ExpectedException(typeof(RedisErrorException), "Not specifying AUTH should fail")] // TODO: Create NoAuthException
		public void connectionWithoutPassword_fails() {
			// TODO: Find a way to put this in ConnectionClient? Or move test to RedisTest?
			using (Redis redis = new Redis(port: 6399)) {
				bool actual = redis.ping();
			}
		}

		[TestMethod]
		public void connectionWithPassword_succeeds() {
			using (Redis redis = new Redis(port: 6399, password: "testing")) {
				bool actual = redis.ping();
				Assert.IsTrue(actual);
			}
		}
	}
}