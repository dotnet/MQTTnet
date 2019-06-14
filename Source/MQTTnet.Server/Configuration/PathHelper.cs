using System;
using System.IO;

namespace MQTTnet.Server.Configuration
{
    public static class PathHelper
    {
        public static string ExpandPath(string path)
        {
            if (path == null)
            {
                return null;
            }

            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            }

            return path;
        }
    }
}
