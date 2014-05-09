using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class ConnectionClientTest {
		[TestMethod]
		public void sendMultipleCommandsBeforeClosing() {
			using (ConnectionClient client = new ConnectionClient()) {
				Command command = Command.fromString("PING\r\n");
				Command command2 = Command.fromString("PING\r\n");
				client.send(command);
				client.send(command2);
			}
		}
	}
}
