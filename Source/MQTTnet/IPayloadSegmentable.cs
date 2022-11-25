namespace MQTTnet
{
    public interface IPayloadSegmentable
    {
        /// <summary>
        /// Get or set Payload array
        /// </summary>
        byte[] Payload { get; set; }

        /// <summary>
        /// Get or set the offset of Payload
        /// </summary>
        int PayloadOffset { get; set; }

        /// <summary>
        /// Get or set the effective number of bytes of Payload
        /// Leaving null means equal to the total length of Payload minus PayloadOffset
        /// </summary>
        int? PayloadCount { get; set; }
    }
}
