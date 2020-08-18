using System;
using System.ServiceProcess;
using Tc.Monitor.Server.DbManagerLib;

namespace Tc.Monitor.Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var service1 = new WindowsService();

            if (ConsoleMode(args))
            {
                service1.StartService();
                Console.ReadLine();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { service1 });
            }
        }

        private static bool ConsoleMode(string[] args)
        {
            return args.Length > 0 && args[0].ToLower() == "console";
        }
    }
}
