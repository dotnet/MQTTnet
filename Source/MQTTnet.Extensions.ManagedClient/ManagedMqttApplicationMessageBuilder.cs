using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttApplicationMessageBuilder
    {
        private Guid _id = Guid.NewGuid();
        private MqttApplicationMessage _applicationMessage;

        public ManagedMqttApplicationMessageBuilder WithId(Guid id)
        {
            _id = id;
            return this;
        }

        public ManagedMqttApplicationMessageBuilder WithApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            _applicationMessage = applicationMessage;
            return this;
        }

        public ManagedMqttApplicationMessageBuilder WithApplicationMessage(Action<MqttApplicationMessageBuilder> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var internalBuilder = new MqttApplicationMessageBuilder();
            builder(internalBuilder);

            _applicationMessage = internalBuilder.Build();
            return this;
        }

        public ManagedMqttApplicationMessage Build()
        {
            if (_applicationMessage == null)
            {
                throw new InvalidOperationException("The ApplicationMessage cannot be null.");
            }

            return new ManagedMqttApplicationMessage
            {
                Id = _id,
                ApplicationMessage = _applicationMessage
            };
        }
    }
}
