using System.Collections.Generic;

namespace MQTTnet.Server.Configuration
{
    public class ScriptingSettingsModel
    {
        public string ScriptsPath { get; set; }

        public List<string> IncludePaths { get; set; }
    }
}
