using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class ByteArrayExtensionsTest {

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void match_bytesAtBeginning_true() {
			byte[] buffer = {1, 2, 3, 4, 5, 6};
			byte[] value = {1, 2, 3};
			Assert.IsTrue(buffer.match(value, 0, false));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtBeginning_0() {
			byte[] buffer = {1, 2, 3, 4, 5, 6};
			byte[] value = {1, 2, 3};
			Assert.AreEqual(0, buffer.indexOf(value));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_1() {
			byte[] buffer = {1, 2, 3, 4, 5, 6};
			byte[] value = {2, 3};
			Assert.AreEqual(1, buffer.indexOf(value));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_notFound() {
			byte[] buffer = {1, 2, 3, 4, 5, 6};
			byte[] value = {2, 3, 5};
			Assert.AreEqual(-1, buffer.indexOf(value));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_wrapAround() {
			byte[] buffer = {1, 2, 3, 4, 5, 6};
			byte[] value = {5, 6, 1};
			Assert.AreEqual(4, buffer.indexOf(value, wrapAround: true));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_noWrapAround() {
			byte[] buffer = { 1, 2, 3, 4, 5, 6 };
			byte[] value = { 5, 6, 1 };
			Assert.AreEqual(-1, buffer.indexOf(value));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_startingIndex() {
			byte[] buffer = {1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4};
			byte[] value = {1, 2, 3};
			Assert.AreEqual(4, buffer.indexOf(value, 1));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_startingIndex_wrapAround() {
			byte[] buffer = {1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4};
			byte[] value = {3, 4, 1, 2};
			Assert.AreEqual(10, buffer.indexOf(value, 8, true));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_bytesAtMiddle_startingIndex_noWrapAround() {
			byte[] buffer = { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 };
			byte[] value = { 3, 4, 1, 2 };
			Assert.AreEqual(-1, buffer.indexOf(value, 8));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_longValue_notFound() {
			byte[] buffer = { 1, 2, 3 };
			byte[] value = { 1, 2, 3, 1, 2, 3 };
			Assert.AreEqual(-1, buffer.indexOf(value));
		}

		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void indexOf_ascii_beginning() {
			// $ 3 \r \n f o o \r \n
			byte[] buffer = {36, 51, 13, 10, 102, 111, 111, 13, 10};
			Assert.AreEqual(0, buffer.indexOf("$", Encoding.ASCII));
		}
	}
}