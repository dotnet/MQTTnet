namespace MQTTnet.Server.Configuration
{
    /// <summary>
    /// Main Settings Model
    /// </summary>
    public class SettingsModel
    {
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
        public TcpEndPointModel TcpEndPoint { get; set; } = new TcpEndPointModel();

        /// <summary>
        /// Encryption Listen Settings
        /// </summary>
        public TcpEndPointModel EncryptedTcpEndPoint { get; set; } = new TcpEndPointModel();

        /// <summary>
        /// Settings for the Web Socket endpoint.
        /// </summary>
        public WebSocketEndPointModel WebSocketEndPoint { get; set; } = new WebSocketEndPointModel();

        /// <summary>
        /// Set limit for max pending messages per client
        /// </summary>
        public int MaxPendingMessagesPerClient { get; set; } = 250;

        /// <summary>
        /// The settings for retained messages.
        /// </summary>
        public RetainedApplicationMessagesModel RetainedApplicationMessages { get; set; } = new RetainedApplicationMessagesModel();
    }
}