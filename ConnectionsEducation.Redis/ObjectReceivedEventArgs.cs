using System;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// Arguments for an objectReceived event
	/// </summary>
	public class ObjectReceivedEventArgs : EventArgs {
		/// <summary>
		/// The object received
		/// </summary>
		private readonly object _objectReceived;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectReceivedEventArgs"/> class.
		/// </summary>
		/// <param name="objectReceived">The object received.</param>
		public ObjectReceivedEventArgs(object objectReceived) {
			_objectReceived = objectReceived;
		}

		/// <summary>
		/// Gets the object.
		/// </summary>
		/// <value>The object.</value>
		public object Object {
			get { return _objectReceived; }
		}
	}
}