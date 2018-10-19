using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class ConnectionContextMockup : ConnectionContext
    {
        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; }

        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }

        public ConnectionContextMockup()
        {
            //Transport = new DefaultConnectionContext
        }
    }
}
