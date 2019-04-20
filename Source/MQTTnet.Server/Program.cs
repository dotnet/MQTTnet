using System;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace MQTTnet.Server
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                PrintLogo();

                WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build().Run();

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
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Red;
            const string LogoText =
@"|                                                                                      |
|      __  __  ____ _______ _______         _      _____                               |
|     |  \/  |/ __ \__   __|__   __|       | |    / ____|                              |
|     | \  / | |  | | | |     | |_ __   ___| |_  | (___   ___ _ ____   _____ _ __      |
|     | |\/| | |  | | | |     | | '_ \ / _ \ __|  \___ \ / _ \ '__\ \ / / _ \ '__|     |
|     | |  | | |__| | | |     | | | | |  __/ |_   ____) |  __/ |   \ V /  __/ |        |
|     |_|  |_|\___\_\ |_|     |_|_| |_|\___|\__| |_____/ \___|_|    \_/ \___|_|        |
|                                                                                      |
|                                                                                      |";

            Console.WriteLine(LogoText);
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"
          -- The official MQTT server implementation of MQTTnet --
                 Copyright (c) 2017-2019 The MQTTnet Team");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@"
                   https://github.com/chkr1011/MQTTnet");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($@"
 Version:    {Assembly.GetExecutingAssembly().GetName().Version}
 License:    MIT (read LICENSE file)
 Sponsoring: https://opencollective.com/mqttnet
 Support:    https://github.com/chkr1011/MQTTnet/issues
 Docs:       https://github.com/chkr1011/MQTTnet/wiki/MQTTnetServer
");

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ! THIS IS AN ALPHA VERSION! IT IS NOT RECOMMENDED TO USE IT FOR ANY DIFFERENT PURPOSE THAN TESTING OR EVALUATING!");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}