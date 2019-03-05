using System.Collections.Generic;

namespace MQTTnet.Server.Configuration
{
    /// <summary>
    /// Main Settings Model
    /// </summary>
    public class SettingsModel
    {
        /// <summary>
        /// Listen Settings
        /// </summary>
        public IEnumerable<ListenModel> Listen { get; set; }
    }
}