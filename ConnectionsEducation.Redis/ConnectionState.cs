using System;
using System.Collections;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ConnectionsEducation.Redis {
	public class ConnectionState {
		private const byte PLUS = 43;
		private const byte COLON = 58;
		private const byte ASTERISK = 42;
		private const byte DOLLAR = 36;
		private const byte MINUS = 45;
		private const byte CR = 13;
		private const byte LF = 10;

		public const int BUFFER_SIZE = 1000;

		private readonly Encoding _encoding;
		private static readonly byte[] CrLf = {CR, LF};

		public byte[] buffer = new byte[BUFFER_SIZE];
		public Queue receivedData = new Queue();
		public Stack operations = new Stack();
		public Socket workSocket = null;

		public event EventHandler<ObjectReceievedEventArgs> objectReceived;

		protected virtual void onObjectReceived(ObjectReceievedEventArgs e) {
			EventHandler<ObjectReceievedEventArgs> handler = objectReceived;
			if (handler != null)
				handler(this, e);
		}

		public ConnectionState() : this(Encoding.ASCII) {}

		public ConnectionState(Encoding encoding) {
			_encoding = encoding;
		}

		public Encoding encoding {
			get { return _encoding; }
		}

		public void update(int bytesRead) {
			int bufferIndex = 0;

			while (bufferIndex < buffer.Length && bufferIndex < bytesRead) {
				if (operations.Count == 0)
					operations.Push(new AwaitCommand());
				object nextOp = operations.Peek();
				if (nextOp is AwaitCommand) {
					switch (buffer[bufferIndex]) {
						case PLUS:
							operations.Pop();
							operations.Push(new AwaitSimpleString());
							++bufferIndex;
							continue;
						case DOLLAR:
							operations.Pop();
							operations.Push(new AwaitString { readSize = false });
							operations.Push(new AwaitSimpleString());
							++bufferIndex;
							continue;
						case COLON:
							operations.Pop();
							operations.Push(new AwaitInt());
							++bufferIndex;
							continue;
						default:
							throw new NotImplementedException("Operation not implemented for " + encoding.GetString(buffer, bufferIndex, 1));
					}
				} else if (nextOp is AwaitSimpleString) {
					int indexCrLf = buffer.indexOf(CrLf, bufferIndex);
					AwaitSimpleString op = (AwaitSimpleString)nextOp;
					if (indexCrLf == -1) {
						op.data = (op.data ?? Enumerable.Empty<byte>()).Concat(buffer.Skip(bufferIndex)).ToArray();
						bufferIndex = buffer.Length;
					} else {
						byte[] stringData = (op.data ?? Enumerable.Empty<byte>()).Concat(buffer.Skip(bufferIndex).Take(indexCrLf - bufferIndex)).ToArray();
						string value = _encoding.GetString(stringData);
						operations.Pop();
						apply(value);
						bufferIndex = indexCrLf + CrLf.Length;
					}
				} else if (nextOp is AwaitInt) {
					int indexCrLf = buffer.indexOf(CrLf, bufferIndex);
					AwaitInt op = (AwaitInt)nextOp;
					if (indexCrLf == -1) {
						op.data = (op.data ?? Enumerable.Empty<byte>()).Concat(buffer.Skip(bufferIndex)).ToArray();
						bufferIndex = buffer.Length;
					} else {
						byte[] stringData = (op.data ?? Enumerable.Empty<byte>()).Concat(buffer.Skip(bufferIndex).Take(indexCrLf - bufferIndex)).ToArray();
						string valueLiteral = _encoding.GetString(stringData);
						operations.Pop();
						long value;
						if (long.TryParse(valueLiteral, out value))
							apply(value);
						else
							apply(new Exception(string.Format("Failed to parse {0} as an integer", valueLiteral)));
						bufferIndex = indexCrLf + CrLf.Length;
					}
				} else if (nextOp is AwaitString) {
					AwaitString op = (AwaitString)nextOp;
					int currentSize = op.data.Length;
					int availableLength = buffer.Length - bufferIndex;
					if (currentSize + availableLength >= op.size) {
						int count = op.size - op.data.Length;
						byte[] stringData = op.data.Concat(buffer.Skip(bufferIndex).Take(count)).ToArray();
						string value = _encoding.GetString(stringData);
						operations.Pop();
						operations.Push(new AwaitCrLf());
						apply(value);
						bufferIndex = bufferIndex + count;
					} else {
						op.data = op.data.Concat(buffer.Skip(bufferIndex).Take(availableLength)).ToArray();
						bufferIndex = bufferIndex + availableLength;
					}
				} else if (nextOp is AwaitCrLf) {
					AwaitCrLf op = (AwaitCrLf)nextOp;
					int availableLength = buffer.Length - bufferIndex;
					if (availableLength < 1)
						continue;
					if (!op.cr) {
						if (availableLength >= 2) {
							if (buffer[bufferIndex] == CR && buffer[bufferIndex + 1] == LF) {
								operations.Pop();
								bufferIndex += 2;
							} else {
								throw new InvalidOperationException("Protocol Violation. Expected CR-LF.");
							}
						} else {
							if (buffer[bufferIndex] == CR) {
								op.cr = true;
								++bufferIndex;
							} else {
								throw new InvalidOperationException("Protocol Violation. Expected CR.");
							}
						}
					} else {
						if (buffer[bufferIndex] == LF) {
							operations.Pop();
							++bufferIndex;
						} else {
							throw new InvalidOperationException("Protocol Violation. Expected LF.");
						}
					}
				} else {
					throw new NotImplementedException("Don't know what to do.");
				}


				if (operations.Count == 0) {
					// Opportunity here to convert from "internal" state to "external" state.
					Queue receivedObject = new Queue(receivedData.Count);
					while (receivedData.Count > 0)
						receivedObject.Enqueue(receivedData.Dequeue());
					onObjectReceived(new ObjectReceievedEventArgs(receivedObject));
				}
			}
		}

		private void apply(string value) {
			if (operations.Count == 0) {
				receivedData.Enqueue(value);
				return;
			}

			object nextOp = operations.Peek();
			if (nextOp is AwaitString && !((AwaitString)nextOp).readSize) {
				AwaitString op = (AwaitString)nextOp;
				int size = Convert.ToInt32(value);
				op.readSize = true;
				op.size = size;
				op.data = new byte[] {};
				if (op.size < 0) {
					operations.Pop();
					apply((string)null);
				}
			} else
				receivedData.Enqueue(value);
		}

		private void apply(long value) {
			if (operations.Count == 0) {
				receivedData.Enqueue(value);
			}
		}

		private void apply(Exception error) {
			if (operations.Count == 0) {
				receivedData.Enqueue(error);
			}
		}

		private class AwaitSimpleString {
			public byte[] data { get; set; }
		}

		private class AwaitInt {
			public byte[] data { get; set; }
		}

		private class AwaitString {
			public bool readSize { get; set; }
			public int size { get; set; }
			public byte[] data { get; set; }
		}

		private class AwaitCommand {
		}

		private class AwaitCrLf {
			public bool cr { get; set; }
		}
	}
}