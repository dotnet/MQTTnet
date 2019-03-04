using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MQTTnet.AspNetCore;
using MQTTnetServer.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace MQTTnetServer
{
    /// <summary>
    /// Main Entry point
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Could not find application settings file in: " + e.FileName);
                return;
            }
        }

        /// <summary>
        /// Configure and Start Kestrel
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(args);
            var listen = ReadListenSettings();
            webHost
                 .UseKestrel(o =>
                 {
                     if (listen?.Length > 0)
                     {
                         foreach (var item in listen)
                         {
                             if (item.Address?.Trim() == "*")
                             {
                                 if (item.Protocol == ListenProtocolTypes.MQTT)
                                 {
                                     o.ListenAnyIP(item.Port, c => c.UseMqtt());
                                 }
                                 else
                                 {
                                     o.ListenAnyIP(item.Port);
                                 }
                             }
                             else if (item.Address?.Trim() == "localhost")
                             {
                                 if (item.Protocol == ListenProtocolTypes.MQTT)
                                 {
                                     o.ListenLocalhost(item.Port, c => c.UseMqtt());
                                 }
                                 else
                                 {
                                     o.ListenLocalhost(item.Port);
                                 }
                             }
                             else
                             {
                                 if (IPAddress.TryParse(item.Address, out var ip))
                                 {
                                     if (item.Protocol == ListenProtocolTypes.MQTT)
                                     {
                                         o.Listen(ip, item.Port, c => c.UseMqtt());
                                     }
                                     else
                                     {
                                         o.Listen(ip, item.Port);
                                     }
                                 }
                             }
                         }
                     }
                     else
                     {
                         o.ListenAnyIP(1883, l => l.UseMqtt());
                         o.ListenAnyIP(5000);
                     }
                 });

            webHost.UseStartup<Startup>();

            return webHost;
        }

        /// <summary>
        /// Read Application Settings
        /// </summary>
        /// <returns></returns>
        public static ListenModel[] ReadListenSettings()
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();

            var listen = new List<ListenModel>();
            builder.Bind("MQTTnetServer:Listen", listen);

            return listen.ToArray();
        }
    }
}
