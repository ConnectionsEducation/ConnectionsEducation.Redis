using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConnectionsEducation.Redis {
	public class ConnectionClient {
		// Credit http://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.100).aspx "Asynchronous Client Socket Example"

		private readonly string _command;
		private readonly ManualResetEventSlim _connectDone = new ManualResetEventSlim(false);
		private readonly Encoding _encoding;
		private readonly string _host;
		private readonly int _port;
		private readonly ManualResetEventSlim _receiveDone = new ManualResetEventSlim(false);
		private readonly ManualResetEventSlim _sendDone = new ManualResetEventSlim(false);
		private readonly int _timeout;

		public ConnectionClient(string command, string host = "127.0.0.1", int port = 6379, int timeout = 60000, Encoding encoding = null) {
			_command = command;
			_host = host;
			_port = port;
			_timeout = timeout;
			_encoding = encoding ?? Encoding.ASCII;
		}

		public Encoding encoding {
			get { return _encoding; }
		}

		public event EventHandler<ObjectReceievedEventArgs> objectReceived;

		protected virtual void onObjectReceived(ObjectReceievedEventArgs e) {
			EventHandler<ObjectReceievedEventArgs> handler = objectReceived;
			if (handler != null)
				handler(this, e);
		}

		public void initiate() {
			IPHostEntry ipHostInfo = Dns.GetHostEntry(_host);
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddress, _port);
			Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			client.BeginConnect(endPoint, new AsyncCallback(connectCallback), client);
			if (!_connectDone.Wait(_timeout))
				throw new TimeoutException("Timeout _connectDone");

			send(client, _command);

			if (!_sendDone.Wait(_timeout))
				throw new TimeoutException("Timeout _sendDone");
			receive(client);
			if (!_receiveDone.Wait(_timeout))
				throw new TimeoutException("Timeout _receiveDone");

			client.Shutdown(SocketShutdown.Both);
			client.Close();
		}

		private void connectCallback(IAsyncResult result) {
			Socket client = (Socket)result.AsyncState;
			client.EndConnect(result);
			_connectDone.Set();
		}

		private void receive(Socket client) {
			ConnectionState state = new ConnectionState(_encoding);
			state.workSocket = client;
			client.BeginReceive(state.buffer, 0, ConnectionState.BUFFER_SIZE, 0, new AsyncCallback(receiveCallback), state);
		}

		private void receiveCallback(IAsyncResult result) {
			ConnectionState state = (ConnectionState)result.AsyncState;
			Socket client = state.workSocket;

			// Read data from the remote device.
			int bytesRead = client.EndReceive(result);
			bool done = false;

			if (bytesRead > 0)
				done = updateState(state, bytesRead);

			if (!done)
				client.BeginReceive(state.buffer, 0, ConnectionState.BUFFER_SIZE, 0, new AsyncCallback(receiveCallback), state);
			else {
				onObjectReceived(new ObjectReceievedEventArgs(state.receivedData));
				// Signal that all bytes have been received.
				_receiveDone.Set();
			}
		}

		private static bool updateState(ConnectionState state, int bytesRead) {
			return state.update(bytesRead);
		}

		private void send(Socket client, String data) {
			byte[] byteData = _encoding.GetBytes(data);
			client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(sendCallback), client);
		}

		private void sendCallback(IAsyncResult result) {
			Socket client = (Socket)result.AsyncState;
			client.EndSend(result);
			_sendDone.Set();
		}
	}
}