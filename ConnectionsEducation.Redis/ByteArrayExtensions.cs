
using System.Text;

namespace ConnectionsEducation.Redis {
	public static class ByteArrayExtensions {
		public static bool match(this byte[] buffer, byte[] valueBytes, int startIndex, bool wrapAround) {
			int i = startIndex;
			int j = 0;
			if (buffer == null || buffer.Length == 0)
				return false;
			while (j < valueBytes.Length) {
				if (i >= buffer.Length) {
					if (wrapAround)
						i = i % buffer.Length;
					else
						return false;
				}
				if (valueBytes[j] != buffer[i])
					return false;
				++i;
				++j;
			}
			return true;
		}

		public static int indexOf(this byte[] buffer, string value, Encoding encoding, int startIndex = 0, bool wrapAround = false) {
			byte[] valueBytes = encoding.GetBytes(value);
			return indexOf(buffer, valueBytes, startIndex, wrapAround);
		}

		public static int indexOf(this byte[] buffer, byte[] valueBytes, int startIndex = 0, bool wrapAround = false) {
			if (buffer == null || buffer.Length == 0 || valueBytes.Length > buffer.Length)
				return -1;
			startIndex = startIndex % buffer.Length;
			int i = startIndex;
			bool match = buffer.match(valueBytes, i, wrapAround);
			if (match)
				return i;
			i = (i + 1) % buffer.Length;
			while (i > startIndex || (wrapAround && i != startIndex)) {
				match = buffer.match(valueBytes, i, wrapAround);
				if (match)
					return i;
				i = (i + 1) % buffer.Length;
			}
			return -1;
		}
	}
}
