// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttApplicationMessageBuilder
    {
        MqttApplicationMessage _applicationMessage;
        string _id = Guid.NewGuid().ToString();

        public ManagedMqttApplicationMessage Build()
        {
            if (_applicationMessage == null)
            {
                throw new InvalidOperationException("The ApplicationMessage cannot be null.");
            }

            return new ManagedMqttApplicationMessage(_id, _applicationMessage);
        }

        public ManagedMqttApplicationMessageBuilder WithApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            _applicationMessage = applicationMessage;
            return this;
        }

        public ManagedMqttApplicationMessageBuilder WithApplicationMessage(Action<MqttApplicationMessageBuilder> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var internalBuilder = new MqttApplicationMessageBuilder();
            builder(internalBuilder);

            _applicationMessage = internalBuilder.Build();
            return this;
        }

        public ManagedMqttApplicationMessageBuilder WithId(Guid id)
        {
            _id = id.ToString();
            return this;
        }

        public ManagedMqttApplicationMessageBuilder WithId(string id)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            return this;
        }
    }
}