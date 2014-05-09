using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	[TestClass]
	public class ConnectionStateTest {
		private Queue _receivedData;
		private ConnectionState _state;

		[TestInitialize]
		public void initialize() {
			_receivedData = new Queue();
			_state = new ConnectionState();
			_state.objectReceived += _state_objectReceived;
		}

		private void _state_objectReceived(object sender, ObjectReceievedEventArgs e) {
			_receivedData.Enqueue(e.Object);
		}

		[TestMethod]
		public void simpleStringAsOnlyBuffer_addsString() {	
			const string EXPECTED = "PONG";
			byte[] protocolData = _state.encoding.GetBytes("+PONG\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			assertString(EXPECTED, _receivedData.Dequeue());
		}

		[TestMethod]
		public void bulkStringAsOnlyBuffer_addsString() {
			const string EXPECTED = "foo bar";
			byte[] protocolData = _state.encoding.GetBytes("$7\r\nfoo bar\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			assertString(EXPECTED, _receivedData.Dequeue());
		}

		private void assertString(string expected, object receivedValue) {
			Queue data = (Queue)receivedValue;
			Assert.IsNotNull(data);
			Assert.AreEqual(1, data.Count, "Data does not contain any objects.");
			object value = data.Dequeue();
			Assert.IsTrue(value is string, "Data is not a string.");
			Assert.AreEqual(expected, value as string);
		}
	}
}
