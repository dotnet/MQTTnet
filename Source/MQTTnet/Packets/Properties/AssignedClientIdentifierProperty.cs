namespace MQTTnet.Packets.Properties
{
    public class AssignedClientIdentifierProperty : StringProperty
    {
        public AssignedClientIdentifierProperty(string value) 
            : base((byte)PropertyID.AssignedClientIdentifer, value)
        {
        }
    }
}
