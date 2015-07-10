using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class ConnectionClientTest {
		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void sendMultipleCommandsBeforeClosing() {
			using (ConnectionClient client = new ConnectionClient()) {
				Command command = Command.fromString("PING\r\n");
				Command command2 = Command.fromString("PING\r\n");
				client.connect();
				client.send(command);
				client.send(command2);
			}
		}
	}
}