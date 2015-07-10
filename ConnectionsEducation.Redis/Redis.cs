using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// A high-level application interface for Redis functionality.
	/// </summary>
	public partial class Redis : IDisposable {
		/// <summary>
		/// The client
		/// </summary>
		private readonly ConnectionClient _client;

		/// <summary>
		/// The results queue
		/// </summary>
		private readonly ConcurrentQueue<AwaitResult> _results = new ConcurrentQueue<AwaitResult>();

		/// <summary>
		/// The result consumed toggle
		/// </summary>
		private readonly ManualResetEventSlim _resultConsumed = new ManualResetEventSlim(true);

		/// <summary>
		/// The encoding of data
		/// </summary>
		private readonly Encoding _encoding;

		/// <summary>
		/// The synchronization lock
		/// </summary>
		private readonly object _lock = new object();

		/// <summary>
		/// Class AwaitResult.
		/// </summary>
		private class AwaitResult {
			/// <summary>
			/// The waiter
			/// </summary>
			private readonly ManualResetEventSlim _waiter;

			/// <summary>
			/// The number of results to expect for the command;
			/// </summary>
			private readonly int _numberOfResults;

			/// <summary>
			/// Tracks the number of resuts actually added, to know when we're done with this object.
			/// </summary>
			private int _resultsAdded = 0;

			/// <summary>
			/// The result object.
			/// </summary>
			private object _result;

			/// <summary>
			/// Initializes a new instance of the <see cref="AwaitResult"/> class.
			/// </summary>
			/// <param name="waiter">The waiter.</param>
			/// <param name="numberOfResults">The number of results to expect for the command.</param>
			public AwaitResult(ManualResetEventSlim waiter, int numberOfResults = 1) {
				_waiter = waiter;
				_numberOfResults = numberOfResults;
				_result = new object[_numberOfResults];
			}

			/// <summary>
			/// Gets the waiter.
			/// </summary>
			/// <value>The waiter.</value>
			public ManualResetEventSlim waiter
			{
				get { return _waiter; }
			}

			/// <summary>
			/// Gets or sets the result.
			/// </summary>
			/// <value>The result.</value>
			public object result
			{
				get { return _result; }
				set
				{
					Queue queue = _result as Queue;
					if (queue != null) {
						Queue valueQueue = value as Queue;
						if (valueQueue != null)
							queue.Enqueue(valueQueue.Dequeue());
						else
							queue.Enqueue(value);
					} else
						_result = value;
					_resultsAdded++;
				}
			}

			/// <summary>
			/// Returns true if all of the results have been acquired.
			/// </summary>
			public bool allResultsAcquired
			{
				get { return _resultsAdded == _numberOfResults; }
			}
		}

		/// <summary>
		/// Result enumerator
		/// </summary>
		private class ResultEnumerator : IEnumerator {
			/// <summary>
			/// The source queue.
			/// </summary>
			private readonly Queue _queue;

			/// <summary>
			/// The current object for the enumerator.
			/// </summary>
			private object _current;

			/// <summary>
			/// Tracks if the state of the object is valid;
			/// </summary>
			private bool _validState;

			/// <summary>
			/// Creates a ResultEnumerator.
			/// </summary>
			/// <param name="result">The source queue.</param>
			public ResultEnumerator(Queue result) {
				_queue = result;
			}

			/// <summary>
			/// Advances the enumerator.
			/// </summary>
			/// <returns>True if <see cref="Current"/> will return the next value; false if the enumeration has ended.</returns>
			public bool MoveNext() {
				if (_queue.Count > 0) {
					_current = _queue.Dequeue();
					_validState = true;
					return true;
				} else {
					_current = null;
					_validState = false;
					return false;
				}
			}

			/// <summary>
			/// Resets the enumerator -- invalid for this object.
			/// </summary>
			/// <exception cref="InvalidOperationException"></exception>
			public void Reset() {
				throw new InvalidOperationException("The enumeration for this object cannot be reset.");
			}

			/// <summary>
			/// Gets the current object in the enumeration.
			/// </summary>
			public object Current
			{
				get
				{
					if (!_validState)
						throw new InvalidOperationException("The enumeration is not valid for the object's current state.");
					return _current;
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Redis"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="port">The port.</param>
		/// <param name="connectTimeout">The connect timeout in milliseconds.</param>
		/// <param name="encoding">The encoding (optional: default ASCII).</param>
		/// <param name="password">The password required by the "requirepass" configuration directive on the server.</param>
		public Redis(string host = "127.0.0.1", int port = 6379, int connectTimeout = 1000, Encoding encoding = null, string password = null) {
			_encoding = encoding ?? Encoding.Default;
			_client = new ConnectionClient(host, port, connectTimeout, this.encoding);
			if (password != null)
				_client.setAuthPassword(password);
			_client.objectReceived += _client_objectReceived;
			_client.connectionError += _client_connectionError;
			_client.connect();
		}

		/// <summary>
		/// The encoding of data
		/// </summary>
		public Encoding encoding
		{
			get { return _encoding; }
		}

		/// <summary>
		/// Handles the connectionError event of the client.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		private void _client_connectionError(object sender, EventArgs e) {
			AwaitResult awaitResult;
			while (_results.TryDequeue(out awaitResult)) {
				awaitResult.result = new ConnectionError();
				awaitResult.waiter.Set();
			}
		}

		/// <summary>
		/// Class ConnectionError.
		/// </summary>
		private class ConnectionError {}

		/// <summary>
		/// The current await-result.
		/// </summary>
		private AwaitResult _awaitResult;

		/// <summary>
		/// Handles the objectReceived event of the client.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ObjectReceivedEventArgs"/> instance containing the event data.</param>
		private void _client_objectReceived(object sender, ObjectReceivedEventArgs e) {
			if (_awaitResult == null) {
				if (!_results.TryDequeue(out _awaitResult))
					_awaitResult = null;
			}
			if (_awaitResult != null) {
				_awaitResult.result = e.Object;
				if (_awaitResult.allResultsAcquired) {
					_awaitResult.waiter.Set();
					_awaitResult = null;
				}
			}
			_resultConsumed.Reset();
		}

		/// <summary>
		/// Send an arbitrary command, returning the result as a string.
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>The string result</returns>
		public string stringCommand(Command command) {
			return resultToString(sendCommand(command), encoding);
		}

		/// <summary>
		/// Send an arbitrary command, returning the result as a number.
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>The numeric result</returns>
		public long numberCommand(Command command) {
			return resultToNumber(sendCommand(command));
		}

		/// <summary>
		/// Send an arbitrary command, returning the result as a array.
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>The array result</returns>
		public object[] arrayCommand(Command command) {
			return resultToArray(sendCommand(command));
		}

		/// <summary>
		/// Send an abitrary command, discarding any result.
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>The result of the command, in its unaltered representation.</returns>
		public object executeCommand(Command command) {
			return result(sendCommand(command));
		}

		/// <summary>
		/// Send an arbitrary command, and enumerate over the results.
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>The results of the command.</returns>
		public IEnumerator enumerateCommand(Command command) {
			return new ResultEnumerator(sendCommand(command));
		}

		/// <summary>
		/// Sends the next command.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <returns>The result of the command.</returns>
		/// <exception cref="System.Exception">Connection Error</exception>
		private Queue sendCommand(Command command) {
			int numberOfResultsToExpect = command.numberOfResultsToExpect;
			ManualResetEventSlim waiter = new ManualResetEventSlim(false);
			AwaitResult awaitResult = new AwaitResult(waiter, numberOfResultsToExpect);
			lock (_lock) {
				_client.send(command);
				_results.Enqueue(awaitResult);
			}

			waiter.Wait();
			object result = awaitResult.result;
			_resultConsumed.Set();
			if (result is ConnectionError)
				throw new Exception("Connection Error");

			return (Queue)result;
		}


		/// <summary>
		/// Results to string. (assumed encoding ASCII)
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.String.</returns>
		private static string resultToString(Queue result) {
			return resultToString(result, Encoding.ASCII);
		}

		/// <summary>
		/// Results to string.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <param name="encoding">The encoding for byte data.</param>
		/// <returns>System.String.</returns>
		private static string resultToString(Queue result, Encoding encoding) {
			object data = result.Dequeue();
			if (data == null)
				return null;
			checkThrow(data);
			return encoding.GetString((byte[])data);
		}

		/// <summary>
		/// Results to object.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.Object.</returns>
		private static object result(Queue result) {
			object data = result.Dequeue();
			checkThrow(data);
			return data;
		}

		/// <summary>
		/// Results to number.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.Int64.</returns>
		private static long resultToNumber(Queue result) {
			object data = result.Dequeue();
			checkThrow(data);
			return Convert.ToInt64(data);
		}

		/// <summary>
		/// Results to array.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.Array.</returns>
		private static object[] resultToArray(Queue result) {
			Array data = result.Dequeue() as Array;
			if (data == null)
				return null;
			checkThrow(data);
			object[] value = new object[data.Length];
			Array.Copy(data, value, value.Length);
			return value;
		}

		/// <summary>
		/// Throw the object if it is a Redis error.
		/// </summary>
		/// <param name="data">The object to check</param>
		private static void checkThrow(object data) {
			if (data is RedisErrorException)
				throw data as RedisErrorException;
		}

		#region Disposable

		/// <summary>
		/// True if disposed
		/// </summary>
		private bool _disposed;

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing) {
			if (_disposed)
				return;

			if (disposing)
				_client.Dispose();

			_disposed = true;
		}

		#endregion
	}
}