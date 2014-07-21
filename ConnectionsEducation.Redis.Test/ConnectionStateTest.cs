using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class ConnectionStateTest {
		/// <summary>
		/// Test helper
		/// </summary>
		private Queue _receivedData;
		/// <summary>
		/// Test helper
		/// </summary>
		private ConnectionState _state;

		/// <summary>
		/// Test
		/// </summary>
		[TestInitialize]
		public void initialize() {
			_receivedData = new Queue();
			_state = new ConnectionState();
			_state.objectReceived += _state_objectReceived;
		}

		/// <summary>
		/// Test helper
		/// </summary>
		/// <param name="sender">The <see cref="ConnectionState"/></param>
		/// <param name="e">The event arguments</param>
		private void _state_objectReceived(object sender, ObjectReceivedEventArgs e) {
			_receivedData.Enqueue(e.Object);
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void simpleStringAsOnlyBuffer_addsString() {	
			const string EXPECTED = "PONG";
			byte[] protocolData = _state.encoding.GetBytes("+PONG\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			assertString(EXPECTED, _receivedData.Dequeue());
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void bulkStringAsOnlyBuffer_addsString() {
			const string EXPECTED = "foo bar";
			byte[] protocolData = _state.encoding.GetBytes("$7\r\nfoo bar\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			assertString(EXPECTED, _receivedData.Dequeue());
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void bulkStringNull_addsNull() {
			byte[] protocolData = _state.encoding.GetBytes("$-1\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			Queue data = (Queue)_receivedData.Dequeue();
			Assert.AreEqual(1, data.Count);
			Assert.IsNull(data.Dequeue());
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void integerAsOnlyBuffer_addsInt() {
			byte[] protocolData = _state.encoding.GetBytes(":123\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			Queue data = (Queue)_receivedData.Dequeue();
			Assert.AreEqual(1, data.Count);
			object value = data.Dequeue();
			Assert.IsTrue(value is long, "Data is not an integer");
			Assert.AreEqual(123L, (long)value);
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void simpleList() {
			byte[] protocolData = _state.encoding.GetBytes("*3\r\n$3\r\nfoo\r\n$3\r\nbar\r\n$3\r\nbaz\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			Queue data = (Queue)_receivedData.Dequeue();
			Assert.AreEqual(1, data.Count);
			object value = data.Dequeue();
			Assert.IsTrue(value is object[], "Data is not a list");
			object[] list = (object[])value;
			Assert.AreEqual(3, list.Length, "Data has wrong length");
			Assert.AreEqual("foo", list[0]);
			Assert.AreEqual("bar", list[1]);
			Assert.AreEqual("baz", list[2]);
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void simpleListWithinList() {
			byte[] protocolData = _state.encoding.GetBytes("*3\r\n$3\r\nfoo\r\n*2\r\n$4\r\nbar1\r\n$4\r\nbar2\r\n$3\r\nbaz\r\n");
			Array.Copy(protocolData, _state.buffer, protocolData.Length);

			_state.update(protocolData.Length);

			Queue data = (Queue)_receivedData.Dequeue();
			Assert.AreEqual(1, data.Count);
			object value = data.Dequeue();
			Assert.IsTrue(value is object[], "Data is not a list");
			object[] list = (object[])value;
			Assert.AreEqual(3, list.Length, "Data has wrong length");
			Assert.AreEqual("foo", list[0]);
			object[] listInList = list[1] as object[];
			Assert.IsNotNull(listInList);
			Assert.AreEqual(2, listInList.Length);
			Assert.AreEqual("bar1", listInList[0]);
			Assert.AreEqual("bar2", listInList[1]);
			Assert.AreEqual("baz", list[2]);
		}

		/// <summary>
		/// Test helper
		/// </summary>
		/// <param name="expected">The expected value</param>
		/// <param name="receivedValue">The received object</param>
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
