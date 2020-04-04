using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server.Web;
using System;
using System.Reflection;

namespace MQTTnet.Server
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                PrintLogo();

                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(serverOptions =>
                        {
                        })
                        .UseStartup<Startup>();

                    }).Build().Run();

                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return -1;
            }
        }

        static void PrintLogo()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            const string LogoText =
@"

███╗   ███╗ ██████╗ ████████╗████████╗███╗   ██╗███████╗████████╗    ███████╗███████╗██████╗ ██╗   ██╗███████╗██████╗ 
████╗ ████║██╔═══██╗╚══██╔══╝╚══██╔══╝████╗  ██║██╔════╝╚══██╔══╝    ██╔════╝██╔════╝██╔══██╗██║   ██║██╔════╝██╔══██╗
██╔████╔██║██║   ██║   ██║      ██║   ██╔██╗ ██║█████╗     ██║       ███████╗█████╗  ██████╔╝██║   ██║█████╗  ██████╔╝
██║╚██╔╝██║██║▄▄ ██║   ██║      ██║   ██║╚██╗██║██╔══╝     ██║       ╚════██║██╔══╝  ██╔══██╗╚██╗ ██╔╝██╔══╝  ██╔══██╗
██║ ╚═╝ ██║╚██████╔╝   ██║      ██║   ██║ ╚████║███████╗   ██║       ███████║███████╗██║  ██║ ╚████╔╝ ███████╗██║  ██║
╚═╝     ╚═╝ ╚══▀▀═╝    ╚═╝      ╚═╝   ╚═╝  ╚═══╝╚══════╝   ╚═╝       ╚══════╝╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝╚═╝  ╚═╝
                                                                                                                      
";

            Console.WriteLine(LogoText);
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("The official MQTT server implementation of MQTTnet");
            Console.WriteLine("Copyright (c) 2017-2020 The MQTTnet Team");
            Console.WriteLine(@"https://github.com/chkr1011/MQTTnet");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($@"
Version:    {Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}
License:    MIT (read LICENSE file)
Sponsoring: https://opencollective.com/mqttnet
Support:    https://github.com/chkr1011/MQTTnet/issues
Docs:       https://github.com/chkr1011/MQTTnet/wiki/MQTTnetServer
");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ! THIS IS AN ALPHA VERSION! IT IS NOT RECOMMENDED TO USE IT FOR ANY DIFFERENT PURPOSE THAN TESTING OR EVALUATING!");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}