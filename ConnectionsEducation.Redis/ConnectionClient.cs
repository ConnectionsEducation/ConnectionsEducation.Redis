using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConnectionsEducation.Redis {
	public class ConnectionClient : IDisposable {
		// Credit http://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.100).aspx "Asynchronous Client Socket Example"

		private readonly Encoding _encoding;
		private readonly string _host;
		private readonly int _port;
		private readonly ManualResetEventSlim _connectDone = new ManualResetEventSlim(false);
		private readonly int _connectTimeout;
		private readonly ConcurrentQueue<Command> _commands;
		private readonly CancellationTokenSource _cancel;
		private readonly Socket _client;

		private bool _connected;

		public ConnectionClient(string host = "127.0.0.1", int port = 6379, int connectTimeout = 1000, Encoding encoding = null) {
			_host = host;
			_port = port;
			_connectTimeout = connectTimeout;
			_encoding = encoding ?? Encoding.ASCII;
			_commands = new ConcurrentQueue<Command>();
			_cancel = new CancellationTokenSource();

			_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public void connect() {
			bool connect = false;

			if (!_connected) {
				_connected = true;
				connect = true;
			}

			if (connect) {
				IPAddress ipAddress;
				if (!IPAddress.TryParse(_host, out ipAddress)) {
					IPHostEntry ipHostInfo = Dns.GetHostEntry(_host);
					ipAddress = ipHostInfo.AddressList[0];
				}
				IPEndPoint endPoint = new IPEndPoint(ipAddress, _port);
				_client.BeginConnect(endPoint, new AsyncCallback(connect_then), _client);
			} else {
				if (!_connectDone.Wait(_connectTimeout))
					throw new TimeoutException("_connectDone");
			}
		}

		private void connect_then(IAsyncResult result) {
			Socket client = (Socket)result.AsyncState;
			try {
				client.EndConnect(result);
			} catch {
				onConnectionError();
				return;
			}
			_connectDone.Set();
			recvData(client);
			sendData(client);
		}

		public Encoding encoding {
			get { return _encoding; }
		}

		public event EventHandler<ObjectReceievedEventArgs> objectReceived;
		public event EventHandler connectionError;

		protected virtual void onConnectionError() {
			EventHandler handler = connectionError;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected virtual void onObjectReceived(ObjectReceievedEventArgs e) {
			EventHandler<ObjectReceievedEventArgs> handler = objectReceived;
			if (handler != null)
				handler(this, e);
		}

		public void send(Command command) {
			_commands.Enqueue(command);
		}

		protected virtual void close() {
			_client.Close();
		}

		private void recvData(Socket client) {
			if (!_connectDone.Wait(_connectTimeout))
				onConnectionError();
			ConnectionState state = new ConnectionState(_encoding);
			state.workSocket = client;
			try {
				client.BeginReceive(state.buffer, 0, ConnectionState.BUFFER_SIZE, 0, new AsyncCallback(recvData_then), state);
			} catch (ObjectDisposedException) {}
		}

		private void recvData_then(IAsyncResult result) {
			if (_disposed || _cancel.IsCancellationRequested)
				onConnectionError();
			ConnectionState state = (ConnectionState)result.AsyncState;
			Socket client = state.workSocket;
			EventHandler<ObjectReceievedEventArgs> handler = new EventHandler<ObjectReceievedEventArgs>(objectReceived);
			int bytesRead;
			state.objectReceived += handler;
			try {
				bytesRead = client.EndReceive(result);
			} catch (ObjectDisposedException) {
				bytesRead = 0;
			}

			if (bytesRead > 0)
				updateState(state, bytesRead);
			state.objectReceived -= handler;

			if (!_disposed && !_cancel.IsCancellationRequested) {
				try {
					client.BeginReceive(state.buffer, 0, ConnectionState.BUFFER_SIZE, 0, new AsyncCallback(recvData_then), state);
				} catch (ObjectDisposedException) {}
			}
		}

		private static void updateState(ConnectionState state, int bytesRead) {
			state.update(bytesRead);
		}

		private static byte[] nextData(Tuple<Socket, ConcurrentQueue<Command>> sendCommands) {
			Command nextCommand;
			if (!sendCommands.Item2.TryDequeue(out nextCommand))
				return new byte[0];
			else
				return nextCommand.getBytes();
		}

		private void sendData(Socket client) {
			if (!_connectDone.Wait(_connectTimeout))
				onConnectionError();
			Tuple<Socket,ConcurrentQueue<Command>> sendCommands = new Tuple<Socket,ConcurrentQueue<Command>>(client, _commands);
			byte[] data = nextData(sendCommands);
			if (data.Length == 0)
				Thread.Sleep(0);
			if (!_disposed && !_cancel.IsCancellationRequested) {
				try {
					client.BeginSend(data, 0, data.Length, 0, new AsyncCallback(sendData_then), sendCommands);
				} catch (ObjectDisposedException) {}
			}
		}

		private void sendData_then(IAsyncResult result) {
			if (_disposed || _cancel.IsCancellationRequested)
				return;
			Tuple<Socket, ConcurrentQueue<Command>> sendCommands = (Tuple<Socket, ConcurrentQueue<Command>>)result.AsyncState;
			Socket client = sendCommands.Item1;
			byte[] data;
			try {
				client.EndSend(result);
				data = nextData(sendCommands);
			} catch (ObjectDisposedException) {
				data = new byte[0];
			}

			if (data.Length == 0)
				Thread.Sleep(0);
			if (!_disposed && !_cancel.IsCancellationRequested) {
				try {
					client.BeginSend(data, 0, data.Length, 0, new AsyncCallback(sendData_then), sendCommands);
				} catch (ObjectDisposedException) { }
			}
		}

		#region Disposable

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private bool _disposed;

		// ReSharper disable once InconsistentNaming
		protected virtual void Dispose(bool disposing) {
			if (_disposed)
				return;

			if (disposing) {
				_cancel.Cancel();

				close();
				_client.Dispose();
				_cancel.Dispose();
			}

			_disposed = true;
		}

		#endregion

	}
}