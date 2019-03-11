namespace MQTTnet.Server.Configuration
{
    public class RetainedApplicationMessagesModel
    {
        public bool Persist { get; set; } = false;

        public int WriteInterval { get; set; } = 10;

        public string Filename { get; set; } = "RetainedApplicationMessages.json";
    }
}
