using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class ConnectionTest {
		[TestMethod]
		public void testPing() {
			Connection connection = new Connection();
			Assert.IsTrue(connection.ping());
		}

		[TestMethod]
		public void testSetGetString() {
			Connection connection = new Connection();
			connection.set("foo", "bar");
			string actual = connection.get("foo");
			Assert.AreEqual("bar", actual);
		}

		[TestMethod]
		public void testSetGetString_largerThanBuffer() {
			string expected = "";
			for (int i = 0; i < ConnectionState.BUFFER_SIZE * 2; ++i) {
				expected += ".";
			}
			expected += "XXX";
			Connection connection = new Connection();
			connection.set("foo", expected);
			string actual = connection.get("foo");
			Assert.AreEqual(expected, actual);
		}
	}
}
