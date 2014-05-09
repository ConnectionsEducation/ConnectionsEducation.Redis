using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace ConnectionsEducation.Redis {
	public class Connection : IDisposable {

		private readonly ConnectionClient _client;
		private readonly ConcurrentQueue<AwaitResult> _results = new ConcurrentQueue<AwaitResult>();
		private readonly ManualResetEventSlim _resultConsumed = new ManualResetEventSlim(true);
		private readonly object _lock = new object();

		private class AwaitResult {
			private readonly ManualResetEventSlim _waiter;
			public AwaitResult(ManualResetEventSlim waiter) {
				_waiter = waiter;
			}

			public ManualResetEventSlim waiter {
				get { return _waiter; }
			}

			public object result { get; set; }
		}

		public Connection() {
			_client = new ConnectionClient();
			_client.objectReceived += _client_objectReceived;
			_client.connectionError += _client_connectionError;
			_client.connect();
		}

		void _client_connectionError(object sender, EventArgs e) {
			AwaitResult awaitResult;
			while (_results.TryDequeue(out awaitResult)) {
				awaitResult.result = new ConnectionError();
				awaitResult.waiter.Set();
			}
		}

		private class ConnectionError {}

		void _client_objectReceived(object sender, ObjectReceievedEventArgs e) {
			AwaitResult awaitResult;
			if (_results.TryDequeue(out awaitResult)) {
				awaitResult.result = e.Object;
				awaitResult.waiter.Set();
			}
			_resultConsumed.Reset();
		}

		public bool ping() {
			Command command = Command.fromString("PING\r\n");
			string pong = resultToString(sendCommand(command));
			return pong == "PONG";
		}

		public void set(string key, string value) {
			Command command = new Command("SET", key, value);
			string ok = resultToString(sendCommand(command));
			if (ok != "OK")
				throw new InvalidOperationException("Set result is not OK!");
		}

		public string get(string key) {
			Command command = new Command("GET", key);
			string value = resultToString(sendCommand(command));
			return value;
		}

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

		private static string resultToString(Queue result) {
			if (result == null || result.Count == 0)
				return null;
			object data = result.Dequeue();
			if (data == null)
				return null;
			return data.ToString();
		}

		#region Disposable

		private bool _disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// ReSharper disable once InconsistentNaming
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