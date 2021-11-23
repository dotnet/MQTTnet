using System;

namespace MQTTnet.Server
{
    public sealed class SessionDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the ID of the session.
        /// </summary>
        public string Id { get; internal set; }
    }
}