using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MQTTnet.Server.Configuration;

namespace MQTTnet.Server
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                PrintLogo();

                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return -1;
            }
        }

        private static void PrintLogo()
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(@"
  __  __  ____ _______ _______         _      _____                          
 |  \/  |/ __ \__   __|__   __|       | |    / ____|                         
 | \  / | |  | | | |     | |_ __   ___| |_  | (___   ___ _ ____   _____ _ __ 
 | |\/| | |  | | | |     | | '_ \ / _ \ __|  \___ \ / _ \ '__\ \ / / _ \ '__|
 | |  | | |__| | | |     | | | | |  __/ |_   ____) |  __/ |   \ V /  __/ |   
 |_|  |_|\___\_\ |_|     |_|_| |_|\___|\__| |_____/ \___|_|    \_/ \___|_|");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"
          -- The official MQTT server implementation of MQTTnet --        
                 Copyright (c) 2017-2019 The MQTTnet Team");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@"
                   https://github.com/chkr1011/MQTTnet");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"
 Version: 1.0.0-alpha1
 License: MIT (read LICENSE file)
");

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ! THIS IS AN ALPHA VERSION! IT IS NOT RECOMMENDED TO USE IT FOR ANY DIFFERENT PURPOSE THAN TESTING OR EVALUATING!");
            Console.WriteLine();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(args)
                .UseKestrel(kestrelOptions =>
                {
                    kestrelOptions.ListenAnyIP(80, listenOptions =>
                    {
                        listenOptions.NoDelay = true;
                    });
                });
            
            //var listen = ReadListenSettings();
            //webHost
            //     .UseKestrel(o =>
            //     {
            //         if (listen?.Length > 0)
            //         {
            //             foreach (var item in listen)
            //             {
            //                 if (item.Address?.Trim() == "*")
            //                 {
            //                     if (item.Protocol == ListenProtocolTypes.MQTT)
            //                     {
            //                         o.ListenAnyIP(item.Port, c => c.UseMqtt());
            //                     }
            //                     else
            //                     {
            //                         o.ListenAnyIP(item.Port);
            //                     }
            //                 }
            //                 else if (item.Address?.Trim() == "localhost")
            //                 {
            //                     if (item.Protocol == ListenProtocolTypes.MQTT)
            //                     {
            //                         o.ListenLocalhost(item.Port, c => c.UseMqtt());
            //                     }
            //                     else
            //                     {
            //                         o.ListenLocalhost(item.Port);
            //                     }
            //                 }
            //                 else
            //                 {
            //                     if (IPAddress.TryParse(item.Address, out var ip))
            //                     {
            //                         if (item.Protocol == ListenProtocolTypes.MQTT)
            //                         {
            //                             o.Listen(ip, item.Port, c => c.UseMqtt());
            //                         }
            //                         else
            //                         {
            //                             o.Listen(ip, item.Port);
            //                         }
            //                     }
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             o.ListenAnyIP(1883, l => l.UseMqtt());
            //             o.ListenAnyIP(5000);
            //         }
            //     });

            webHost.UseStartup<Startup>();

            return webHost;
        }

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
