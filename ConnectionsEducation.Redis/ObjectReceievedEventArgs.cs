using System;

namespace ConnectionsEducation.Redis {
	public class ObjectReceievedEventArgs : EventArgs {
		private readonly object _objectReceived;

		public ObjectReceievedEventArgs(object objectReceived) {
			_objectReceived = objectReceived;
		}

		public object Object {
			get { return _objectReceived; }
		}
	}
}