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
			/// Initializes a new instance of the <see cref="AwaitResult"/> class.
			/// </summary>
			/// <param name="waiter">The waiter.</param>
			public AwaitResult(ManualResetEventSlim waiter) {
				_waiter = waiter;
			}

			/// <summary>
			/// Gets the waiter.
			/// </summary>
			/// <value>The waiter.</value>
			public ManualResetEventSlim waiter {
				get { return _waiter; }
			}

			/// <summary>
			/// Gets or sets the result.
			/// </summary>
			/// <value>The result.</value>
			public object result { get; set; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Redis"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="port">The port.</param>
		/// <param name="connectTimeout">The connect timeout in milliseconds.</param>
		/// <param name="encoding">The encoding (optional: default ASCII).</param>
		public Redis(string host = "127.0.0.1", int port = 6379, int connectTimeout = 1000, Encoding encoding = null) {
			_client = new ConnectionClient(host, port, connectTimeout, encoding);
			_client.objectReceived += _client_objectReceived;
			_client.connectionError += _client_connectionError;
			_client.connect();
		}

		/// <summary>
		/// Handles the connectionError event of the client.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		void _client_connectionError(object sender, EventArgs e) {
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
		/// Handles the objectReceived event of the client.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ObjectReceivedEventArgs"/> instance containing the event data.</param>
		void _client_objectReceived(object sender, ObjectReceivedEventArgs e) {
			AwaitResult awaitResult;
			if (_results.TryDequeue(out awaitResult)) {
				awaitResult.result = e.Object;
				awaitResult.waiter.Set();
			}
			_resultConsumed.Reset();
		}

		/// <summary>
		/// Sends the next command.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <returns>The result of the command.</returns>
		/// <exception cref="System.Exception">Connection Error</exception>
		private Queue sendCommand(Command command) {
			ManualResetEventSlim waiter = new ManualResetEventSlim(false);
			AwaitResult awaitResult = new AwaitResult(waiter);
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
		/// Results to string.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.String.</returns>
		private static string resultToString(Queue result) {
			object data = result.Dequeue();
			if (data == null)
				return null;
			return data.ToString();
		}

		/// <summary>
		/// Results to number.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.Int64.</returns>
		private static long resultToNumber(Queue result) {
			object data = result.Dequeue();
			return Convert.ToInt64(data);
		}

		/// <summary>
		/// Results to array.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>System.Array.</returns>
		private static object[] resultToArray(Queue result) {
			Array data = result.Dequeue() as Array;
			object[] value = new object[data.Length];
			Array.Copy(data, value, value.Length);
			return value;
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

			if (disposing) {
				_client.Dispose();
			}

			_disposed = true;
		}

		#endregion
	}
}