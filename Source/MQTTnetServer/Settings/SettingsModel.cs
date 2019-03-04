using System.Collections.Generic;

namespace MQTTnetServer.Settings
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