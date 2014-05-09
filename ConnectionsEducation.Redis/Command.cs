using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectionsEducation.Redis {
	public class Command {
		private readonly byte[] _bytes;

		public static Command fromString(string command, Encoding encoding = null) {
			return new Command((encoding ?? Encoding.ASCII).GetBytes(command));
		}

		private Command(byte[] bytes) {
			_bytes = bytes;
		}

		public Command(Encoding encoding, string command, params string[] args) : this(encoding, command, args, args.Length + 1) {
			
		}

		public Command(string command, params string[] args) : this(Encoding.ASCII, command, args, args.Length + 1) {
			
		}

		private Command(Encoding encoding, string command1, IEnumerable<string> commandArgs, int count) {
			string[] command = command1 == null ? new string[] {} : new string[] {command1};
			IEnumerable<byte> commands = command.Concat(commandArgs).Select(arg => {
				byte[] bytes = encoding.GetBytes(arg);
				return Encoding.ASCII.GetBytes(string.Format("${0}\r\n", bytes.Length))
					.Concat(bytes)
					.Concat(Encoding.ASCII.GetBytes("\r\n"));
			}).Aggregate(Enumerable.Empty<byte>(), (all, bytes) => all.Concat(bytes));
			_bytes = Encoding.ASCII.GetBytes(string.Format("*{0}\r\n", count))
				.Concat(commands).ToArray();
		}

		public byte[] getBytes() {
			byte[] bytes = (byte[])_bytes.Clone();
			return bytes;
		}
	}
}
