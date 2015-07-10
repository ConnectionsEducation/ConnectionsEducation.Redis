using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// Connection state object. Does the heavy-lifting for the Redis protocol by implementing a state machine.
	/// </summary>
	public class ConnectionState {
		/// <summary>
		/// The plus as a byte representation in ASCII
		/// </summary>
		private const byte PLUS = 43;
		/// <summary>
		/// The colon as a byte representation in ASCII
		/// </summary>
		private const byte COLON = 58;
		/// <summary>
		/// The asterisk as a byte representation in ASCII
		/// </summary>
		private const byte ASTERISK = 42;
		/// <summary>
		/// The dollar as a byte representation in ASCII
		/// </summary>
		private const byte DOLLAR = 36;
		/// <summary>
		/// The minus as a byte representation in ASCII
		/// </summary>
		private const byte MINUS = 45;
		/// <summary>
		/// The carriage return as a byte representation in ASCII
		/// </summary>
		private const byte CR = 13;
		/// <summary>
		/// The line-feed as a byte representation in ASCII
		/// </summary>
		private const byte LF = 10;

		/// <summary>
		/// The buffer size
		/// </summary>
		public const int BUFFER_SIZE = 1000;

		/// <summary>
		/// The encoding
		/// </summary>
		private readonly Encoding _encoding;
		/// <summary>
		/// CR-LF as a byte array
		/// </summary>
		private static readonly byte[] CrLf = {CR, LF};

		/// <summary>
		/// The buffer
		/// </summary>
		public byte[] buffer = new byte[BUFFER_SIZE];
		/// <summary>
		/// The received data queue
		/// </summary>
		public Queue receivedData = new Queue();
		/// <summary>
		/// The operations stack
		/// </summary>
		public Stack operations = new Stack();
		/// <summary>
		/// The socket
		/// </summary>
		public Socket workSocket = null;

		private Action<ObjectReceivedEventArgs> _onObjectReceived;

		/// <summary>
		/// A consumer of this <see cref="ConnectionState"/> instance will observe for objects received from the state.
		/// </summary>
		/// <param name="action">The outer scope action to perform. Set to null when finised.</param>
		internal void setObjectReceivedAction(Action<ObjectReceivedEventArgs> action) {
			_onObjectReceived = action;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionState"/> class.
		/// </summary>
		public ConnectionState() : this(Encoding.ASCII) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionState"/> class.
		/// </summary>
		/// <param name="encoding">The encoding.</param>
		public ConnectionState(Encoding encoding) {
			_encoding = encoding;
		}

		/// <summary>
		/// Gets the encoding.
		/// </summary>
		/// <value>The encoding.</value>
		public Encoding encoding {
			get { return _encoding; }
		}

		/// <summary>
		/// Updates the internal states based on the latest bytes read.
		/// </summary>
		/// <param name="bytesRead">The number of bytes read.</param>
		/// <exception cref="System.NotImplementedException">
		/// Operation not implemented for  + encoding.GetString(buffer, bufferIndex, 1)
		/// or
		/// Don't know what to do.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Protocol Violation. Expected CR-LF.
		/// or
		/// Protocol Violation. Expected CR.
		/// or
		/// Protocol Violation. Expected LF.
		/// </exception>
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
						case ASTERISK:
							operations.Pop();
							operations.Push(new AwaitList {readLength = false});
							operations.Push(new AwaitSimpleString());
							++bufferIndex;
							continue;
						case MINUS:
							operations.Pop();
							operations.Push(new AwaitError());
							operations.Push(new AwaitSimpleString());
							++bufferIndex;
							continue;
						default:
							throw new NotImplementedException("Operation not implemented for " + encoding.GetString(buffer, bufferIndex, 1));
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
				} else if (nextOp is AwaitSimpleString) {
					AwaitSimpleString op = (AwaitSimpleString)nextOp;
					IEnumerable<byte> partialBuffer = buffer.Skip(bufferIndex).Take(bytesRead - bufferIndex);
					byte[] potential = op.data == null ? partialBuffer.ToArray() : op.data.Concat(partialBuffer).ToArray();
					int indexCrLf = potential.indexOf(CrLf);
					if (indexCrLf == -1) {
						op.data = potential;
						bufferIndex = buffer.Length;
					} else {
						byte[] stringData = potential.Take(indexCrLf).ToArray();
						operations.Pop();
						if (operations.Count > 0 && operations.Peek() is AwaitError) {
							operations.Pop();
							apply(new RedisErrorException(encoding.GetString(stringData)));
						} else {
							applyString(stringData);
						}
						bufferIndex += indexCrLf + CrLf.Length - (op.data == null ? 0 : op.data.Length);
					}
				} else if (nextOp is AwaitInt) {
					AwaitInt op = (AwaitInt)nextOp;
					IEnumerable<byte> partialBuffer = buffer.Skip(bufferIndex).Take(bytesRead - bufferIndex);
					byte[] potential = op.data == null ? partialBuffer.ToArray() : op.data.Concat(partialBuffer).ToArray();
					int indexCrLf = potential.indexOf(CrLf);
					if (indexCrLf == -1) {
						op.data = potential;
						bufferIndex = buffer.Length;
					} else {
						byte[] stringData = potential.Take(indexCrLf).ToArray();
						string valueLiteral = _encoding.GetString(stringData);
						operations.Pop();
						long value;
						if (long.TryParse(valueLiteral, out value)) {
							apply(value);
						} else {
							apply(new Exception(string.Format("Failed to parse {0} as an integer", valueLiteral)));
						}
						bufferIndex += indexCrLf + CrLf.Length - (op.data == null ? 0 : op.data.Length);
					}
				} else if (nextOp is AwaitString) {
					AwaitString op = (AwaitString)nextOp;
					int currentSize = op.data.Length;
					int availableLength = buffer.Length - bufferIndex;
					if (currentSize + availableLength >= op.size) {
						int count = op.size - op.data.Length;
						byte[] stringData = op.data.Concat(buffer.Skip(bufferIndex).Take(count)).ToArray();
						operations.Pop();
						applyString(stringData);
						bufferIndex = bufferIndex + count;
						operations.Push(new AwaitCrLf());
						continue;
					} else {
						op.data = op.data.Concat(buffer.Skip(bufferIndex).Take(availableLength)).ToArray();
						bufferIndex = bufferIndex + availableLength;
					}
				} else if (nextOp is AwaitList) {
					AwaitList op = (AwaitList)nextOp;
					if (op.objectsRead < op.length)
						operations.Push(new AwaitCommand());
				} else {
					throw new NotImplementedException("Don't know what to do.");
				}


				nextOp = operations.Count > 0 ? operations.Peek() : null;
				while (nextOp is AwaitList && ((AwaitList)nextOp).objectsRead >= ((AwaitList)nextOp).length) {
					operations.Pop();
					apply(((AwaitList)nextOp).data);
					nextOp = operations.Count > 0 ? operations.Peek() : null;
				}

				if (operations.Count == 0) {
					// Opportunity here to convert from "internal" state to "external" state.
					Queue receivedObject = new Queue(receivedData.Count);
					while (receivedData.Count > 0)
						receivedObject.Enqueue(receivedData.Dequeue());
					_onObjectReceived(new ObjectReceivedEventArgs(receivedObject));
				}
			}
		}

		/// <summary>
		/// Applies the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		private void apply(object[] value) {
			if (operations.Count == 0 || !applyToList(value))
				receivedData.Enqueue(value);
		}

		/// <summary>
		/// Applies the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		private void applyString(byte[] value) {
			if (operations.Count == 0) {
				receivedData.Enqueue(value);
				return;
			}

			object nextOp = operations.Peek();
			if (nextOp is AwaitString && !((AwaitString)nextOp).readSize) {
				AwaitString op = (AwaitString)nextOp;
				int size = Convert.ToInt32(Encoding.ASCII.GetString(value));
				op.readSize = true;
				op.size = size;
				op.data = new byte[] {};
				if (op.size < 0) {
					operations.Pop();
					applyString(null);
				}
			} else if (nextOp is AwaitList && !((AwaitList)nextOp).readLength) {
				AwaitList op = (AwaitList)nextOp;
				int length = Convert.ToInt32(Encoding.ASCII.GetString(value));
				op.readLength = true;
				op.length = length;
				if (op.length < 0) {
					operations.Pop();
					applyString(null);
				}
				op.data = new object[length];
			} else {
				if (!applyToList(value)) {
					receivedData.Enqueue(value);
					operations.Push(new AwaitCrLf());
				}
			}
		}

		/// <summary>
		/// Applies to list.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns><c>true</c> if the object was applied to a list, <c>false</c> if another application should be attempted.</returns>
		private bool applyToList(object value) {
			object nextOp = operations.Peek();
			if (!(nextOp is AwaitList))
				return false;
			AwaitList op = (AwaitList)nextOp;
			if (op.objectsRead >= op.length) {
				return false;
			} else {
				op.data[op.objectsRead++] = value;
				return true;
			}
		}

		/// <summary>
		/// Applies the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		private void apply(long value) {
			if (operations.Count == 0 || !applyToList(value)) {
				receivedData.Enqueue(value);
			}
		}

		/// <summary>
		/// Applies the specified error.
		/// </summary>
		/// <param name="error">The error.</param>
		private void apply(Exception error) {
			if (operations.Count == 0 || !applyToList(error)) {
				receivedData.Enqueue(error);
			}
		}

		/// <summary>
		/// Class AwaitSimpleString.
		/// </summary>
		private class AwaitSimpleString {
			/// <summary>
			/// Gets or sets the data.
			/// </summary>
			/// <value>The data.</value>
			public byte[] data { get; set; }
		}

		/// <summary>
		/// Class AwaitInt.
		/// </summary>
		private class AwaitInt {
			/// <summary>
			/// Gets or sets the data.
			/// </summary>
			/// <value>The data.</value>
			public byte[] data { get; set; }
		}

		/// <summary>
		/// Class AwaitString.
		/// </summary>
		private class AwaitString {
			/// <summary>
			/// Gets or sets a value indicating whether the object read the size.
			/// </summary>
			/// <value><c>true</c> if the object read the size; otherwise, <c>false</c>.</value>
			public bool readSize { get; set; }
			/// <summary>
			/// Gets or sets the size.
			/// </summary>
			/// <value>The size.</value>
			public int size { get; set; }
			/// <summary>
			/// Gets or sets the data.
			/// </summary>
			/// <value>The data.</value>
			public byte[] data { get; set; }
		}

		/// <summary>
		/// Class AwaitList.
		/// </summary>
		private class AwaitList {
			/// <summary>
			/// Gets or sets a value indicating whether the object read the length.
			/// </summary>
			/// <value><c>true</c> if the object read the length; otherwise, <c>false</c>.</value>
			public bool readLength { get; set; }
			/// <summary>
			/// Gets or sets the length.
			/// </summary>
			/// <value>The length.</value>
			public int length { get; set; }
			/// <summary>
			/// Gets or sets the objects read.
			/// </summary>
			/// <value>The objects read.</value>
			public int objectsRead { get; set; }
			/// <summary>
			/// Gets or sets the data.
			/// </summary>
			/// <value>The data.</value>
			public object[] data { get; set; }
		}

		/// <summary>
		/// Class AwaitCommand.
		/// </summary>
		private class AwaitCommand {
		}

		/// <summary>
		/// Class AwaitError.
		/// </summary>
		private class AwaitError {
		}

		/// <summary>
		/// Class AwaitCrLf.
		/// </summary>
		private class AwaitCrLf {
			/// <summary>
			/// Gets or sets a value indicating whether a carriage return was read.
			/// </summary>
			/// <value><c>true</c> if CR was read; otherwise, <c>false</c>.</value>
			public bool cr { get; set; }
		}
	}
}