namespace MQTTnetServer.Settings
{
    /// <summary>
    /// Listen Entry Settings Model
    /// </summary>
    public class ListenModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ListenModel()
        {
        }

        /// <summary>
        /// Listen Address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Listen Port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Protocol Type
        /// </summary>
        public ListenProtocolTypes Protocol { get; set; } = ListenProtocolTypes.HTTP;
    }
}