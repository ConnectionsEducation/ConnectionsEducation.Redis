using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// Connection client -- does the heavy-lifting of communicating with the redis server.
	/// </summary>
	public class ConnectionClient : IDisposable {
		// Credit http://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.100).aspx "Asynchronous Client Socket Example"

		/// <summary>
		/// Encoding
		/// </summary>
		private readonly Encoding _encoding;
		/// <summary>
		/// Host
		/// </summary>
		private readonly string _host;
		/// <summary>
		/// Port
		/// </summary>
		private readonly int _port;
		/// <summary>
		/// Connect is done
		/// </summary>
		private readonly ManualResetEventSlim _connectDone = new ManualResetEventSlim(false);
		/// <summary>
		/// Connect timeout in milliseconds
		/// </summary>
		private readonly int _connectTimeout;
		/// <summary>
		/// Command queue
		/// </summary>
		private readonly ConcurrentQueue<Command> _commands;
		/// <summary>
		/// Cancellation
		/// </summary>
		private readonly CancellationTokenSource _cancel;
		/// <summary>
		/// Client socket
		/// </summary>
		private readonly Socket _client;
		/// <summary>
		/// Connected
		/// </summary>
		private bool _connected;

		/// <summary>
		/// Initializes a <see cref="ConnectionClient"/>.
		/// </summary>
		/// <param name="host">The host</param>
		/// <param name="port">The port</param>
		/// <param name="connectTimeout">The connection timeout in milliseconds</param>
		/// <param name="encoding">The encoding to use (optional: default ASCII)</param>
		public ConnectionClient(string host = "127.0.0.1", int port = 6379, int connectTimeout = 1000, Encoding encoding = null) {
			_host = host;
			_port = port;
			_connectTimeout = connectTimeout;
			_encoding = encoding ?? Encoding.ASCII;
			_commands = new ConcurrentQueue<Command>();
			_cancel = new CancellationTokenSource();

			_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}
		
		/// <summary>
		/// Connects
		/// </summary>
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

		/// <summary>
		/// Connect, part II
		/// </summary>
		/// <param name="result">Async result</param>
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

		/// <summary>
		/// Gets the encoding
		/// </summary>
		public Encoding encoding {
			get { return _encoding; }
		}

		/// <summary>
		/// Receives completed result objects "from the wire"
		/// </summary>
		public event EventHandler<ObjectReceivedEventArgs> objectReceived;
		/// <summary>
		/// Event raised when a connection error occurs.
		/// </summary>
		public event EventHandler connectionError;

		/// <summary>
		/// Event invovator for <see cref="connectionError"/>.
		/// </summary>
		protected virtual void onConnectionError() {
			EventHandler handler = connectionError;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		/// <summary>
		/// Event invocator for <see cref="objectReceived"/>
		/// </summary>
		/// <param name="e">Event arguments</param>
		protected virtual void onObjectReceived(ObjectReceivedEventArgs e) {
			EventHandler<ObjectReceivedEventArgs> handler = objectReceived;
			if (handler != null)
				handler(this, e);
		}

		/// <summary>
		/// Queues a <see cref="Command"/> for sending over the wire.
		/// </summary>
		/// <param name="command">The command.</param>
		public void send(Command command) {
			_commands.Enqueue(command);
		}

		/// <summary>
		/// Closes the client.
		/// </summary>
		protected virtual void close() {
			_client.Close();
		}

		/// <summary>
		/// Handler for receiving data.
		/// </summary>
		/// <param name="client">The socket to receive on.</param>
		private void recvData(Socket client) {
			if (!_connectDone.Wait(_connectTimeout))
				onConnectionError();
			ConnectionState state = new ConnectionState(_encoding);
			state.workSocket = client;
			try {
				client.BeginReceive(state.buffer, 0, ConnectionState.BUFFER_SIZE, 0, new AsyncCallback(recvData_then), state);
			} catch (ObjectDisposedException) {}
		}

		/// <summary>
		/// <see cref="recvData"/>, part II.
		/// </summary>
		/// <param name="result">The async result.</param>
		private void recvData_then(IAsyncResult result) {
			if (_disposed || _cancel.IsCancellationRequested)
				onConnectionError();
			ConnectionState state = (ConnectionState)result.AsyncState;
			Socket client = state.workSocket;
			EventHandler<ObjectReceivedEventArgs> handler = new EventHandler<ObjectReceivedEventArgs>(objectReceived);
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

		/// <summary>
		/// Advance the buffer in the state object.
		/// </summary>
		/// <param name="state">The state object</param>
		/// <param name="bytesRead">The number of bytes read from the socket.</param>
		private static void updateState(ConnectionState state, int bytesRead) {
			state.update(bytesRead);
		}

		/// <summary>
		/// Gets the next bytes to send over the wire.
		/// </summary>
		/// <param name="sendCommands">The queue of commands to send from.</param>
		/// <returns>The bytes to send over the wire.</returns>
		private static byte[] nextData(Tuple<Socket, ConcurrentQueue<Command>> sendCommands) {
			Command nextCommand;
			if (!sendCommands.Item2.TryDequeue(out nextCommand))
				return new byte[0];
			else
				return nextCommand.getBytes();
		}

		/// <summary>
		/// Sends data over the wire.
		/// </summary>
		/// <param name="client">The socket.</param>
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

		/// <summary>
		/// <see cref="sendData"/>, part II.
		/// </summary>
		/// <param name="result">The async result.</param>
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

		/// <summary>
		/// Disposes the object and any unmanaged resources.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// True if the object was disposed.
		/// </summary>
		private bool _disposed;

		
		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// Disposes the object and any unmanaged resources.
		/// </summary>
		/// <param name="disposing">True if disposing from IDisposable.Dispose.</param>
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