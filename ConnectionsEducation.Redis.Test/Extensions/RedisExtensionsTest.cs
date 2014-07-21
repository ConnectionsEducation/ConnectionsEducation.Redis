using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionsEducation.Redis.Extensions.Test {
	/// <summary>
	/// Test
	/// </summary>
	[TestClass]
	public class RedisExtensionsTest {
		/// <summary>
		/// Test
		/// </summary>
		[TestMethod]
		public void testGetSetBytes() {
			using (Redis redis = new Redis()) {
				byte[] expected = Guid.NewGuid().ToByteArray().Concat(new byte[] { 0,1,2,3,4,5,6,7,8,9,0,0,255,255,255,255,31,30,29,28,27,26,25,24,23,22,21,20,19,18,17,16,15,14,13,12,11,10,127,128,129}).ToArray();
				redis.setBytes("foobytes", expected);
				byte[] actual = redis.getBytes("foobytes");
				CollectionAssert.AreEqual(expected, actual);
			}
		}
	}
}
