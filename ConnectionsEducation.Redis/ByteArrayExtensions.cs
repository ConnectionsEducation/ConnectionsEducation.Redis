
using System.Text;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// Extensions to byte[] to support stream operations
	/// </summary>
	public static class ByteArrayExtensions {
		/// <summary>
		/// A "contains" operation for byte[].
		/// </summary>
		/// <param name="buffer">The "haystack"</param>
		/// <param name="valueBytes">The "needle"</param>
		/// <param name="startIndex">The index in the haystack to begin the search</param>
		/// <param name="wrapAround">True to start at the beginning if no match is found between <paramref name="startIndex"/> and the end.</param>
		/// <returns>True if a match is found; otherwise, false.</returns>
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

		/// <summary>
		/// An "index-of" operation for byte[].
		/// </summary>
		/// <param name="buffer">The "haystack"</param>
		/// <param name="value">The "needle"</param>
		/// <param name="encoding">The encoding used to transform a string into bytes.</param>
		/// <param name="startIndex">The index in the haystack to begin the search</param>
		/// <param name="wrapAround">True to start at the beginning if no match is found between <paramref name="startIndex"/> and the end.</param>
		/// <returns>The index of a match if found; otherwise, -1.</returns>
		public static int indexOf(this byte[] buffer, string value, Encoding encoding, int startIndex = 0, bool wrapAround = false) {
			byte[] valueBytes = encoding.GetBytes(value);
			return indexOf(buffer, valueBytes, startIndex, wrapAround);
		}


		/// <summary>
		/// An "index-of" operation for byte[].
		/// </summary>
		/// <param name="buffer">The "haystack"</param>
		/// <param name="valueBytes">The "needle"</param>
		/// <param name="startIndex">The index in the haystack to begin the search</param>
		/// <param name="wrapAround">True to start at the beginning if no match is found between <paramref name="startIndex"/> and the end.</param>
		/// <returns>The index of a match if found; otherwise, -1.</returns>
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
