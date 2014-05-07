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

		public Command(Encoding encoding, params string[] args) : this(encoding, args as ICollection<string>) {
			
		}

		public Command(params string[] args) : this(args as ICollection<string>) {
			
		}

		public Command(ICollection<string> commandArgs) : this(Encoding.ASCII, commandArgs) {
			
		}

		private Command(Encoding encoding, ICollection<string> commandArgs) {
			IEnumerable<byte> commands = commandArgs.Select(command => {
				byte[] bytes = encoding.GetBytes(command);
				return Encoding.ASCII.GetBytes(string.Format("${0}\r\n", bytes.Length))
					.Concat(bytes)
					.Concat(Encoding.ASCII.GetBytes("\r\n"));
			}).Aggregate(Enumerable.Empty<byte>(), (all, bytes) => all.Concat(bytes));
			_bytes = Encoding.ASCII.GetBytes(string.Format("*{0}\r\n", commandArgs.Count))
				.Concat(commands).ToArray();
		}

		public byte[] getBytes() {
			byte[] bytes = (byte[])_bytes.Clone();
			return bytes;
		}
	}
}
