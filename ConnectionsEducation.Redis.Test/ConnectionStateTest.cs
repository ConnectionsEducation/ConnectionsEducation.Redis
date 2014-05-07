using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class ConnectionStateTest {

		[TestMethod]
		public void simpleStringAsOnlyBuffer_addsString() {
			ConnectionState state = new ConnectionState();
			const string EXPECTED = "PONG";
			byte[] protocolData = state.encoding.GetBytes("+PONG\r\n");
			Array.Copy(protocolData, state.buffer, protocolData.Length);

			state.update(protocolData.Length);

			assertString(EXPECTED, state);
		}

		[TestMethod]
		public void bulkStringAsOnlyBuffer_addsString() {
			ConnectionState state = new ConnectionState();
			const string EXPECTED = "foo bar";
			byte[] protocolData = state.encoding.GetBytes("$7\r\nfoo bar\r\n");
			Array.Copy(protocolData, state.buffer, protocolData.Length);

			state.update(protocolData.Length);

			assertString(EXPECTED, state);
		}

		private void assertString(string expected, ConnectionState state) {
			Queue data = state.receivedData;
			Assert.IsNotNull(data);
			Assert.AreEqual(1, data.Count, "Data does not contain any objects.");
			object value = data.Dequeue();
			Assert.IsTrue(value is string, "Data is not a string.");
			Assert.AreEqual(expected, value as string);
		}
	}
}
