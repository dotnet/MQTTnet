using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MQTTnet.Server.Web
{
    public static class Extensions
    {
        public static string ReadBodyAsString(this HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
