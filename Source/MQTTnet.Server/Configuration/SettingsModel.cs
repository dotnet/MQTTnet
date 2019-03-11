namespace MQTTnet.Server.Configuration
{
    /// <summary>
    /// Main Settings Model
    /// </summary>
    public class SettingsModel
    {
        public SettingsModel()
        {
        }

        /// <summary>
        /// Set default connection timeout in seconds
        /// </summary>
        public int CommunicationTimeout { get; set; } = 15;

        /// <summary>
        /// Set 0 to disable connection backlogging
        /// </summary>
        public int ConnectionBacklog { get; set; }

        /// <summary>
        /// Enable support for persistent sessions
        /// </summary>
        public bool EnablePersistentSessions { get; set; } = false;

        /// <summary>
        /// Listen Settings
        /// </summary>
        public TcpEndpointModel TcpEndPoint { get; set; } = new TcpEndpointModel();

        /// <summary>
        /// Encryption Listen Settings
        /// </summary>
        public TcpEndpointModel EncryptedTcpEndPoint { get; set; } = new TcpEndpointModel();

        /// <summary>
        /// Settings for the Web Socket endpoint.
        /// </summary>
        public WebSocketEndpointModel WebSocketEndpoint { get; set; } = new WebSocketEndpointModel();

        /// <summary>
        /// Set limit for max pending messages per client
        /// </summary>
        public int MaxPendingMessagesPerClient { get; set; } = 250;
    }
}