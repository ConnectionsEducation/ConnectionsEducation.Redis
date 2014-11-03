namespace ConnectionsEducation.Redis {
	/// <summary>
	/// A high-level application interface for Redis functionality.
	/// </summary>
	public partial class Redis {

		/// <summary>
		/// Serves as the result and iterator of a SCAN command
		/// </summary>
		public class ScanResult {
			/// <summary>
			/// The original cursor value
			/// </summary>
			private readonly long _cursor;
			/// <summary>
			/// The next cursor value
			/// </summary>
			private readonly long _nextCursor;
			/// <summary>
			/// The results from the command
			/// </summary>
			private readonly string[] _results;

			/// <summary>
			/// Initialize a new scan result
			/// </summary>
			/// <param name="cursor">The original cursor value</param>
			/// <param name="nextCursor">The next cursor value</param>
			/// <param name="results">The results from the command</param>
			public ScanResult(long cursor, long nextCursor, string[] results) {
				_cursor = cursor;
				_nextCursor = nextCursor;
				_results = results;
			}

			/// <summary>
			/// The original cursor value
			/// </summary>
			public long cursor {
				get { return _cursor; }
			}

			/// <summary>
			/// The new cursor value. Pass this back into the original SCAN function to get the next set of results.
			/// </summary>
			public long nextCursor {
				get { return _nextCursor; }
			}

			/// <summary>
			/// The results returned from the current iteration
			/// </summary>
			public string[] results {
				get { return _results; }
			}
		}
	}
}
