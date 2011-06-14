/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using SmtpProxy.Properties;

namespace SmtpProxy
{
    class Program
    {
        public readonly static TraceSource Trace = new TraceSource("SmtpProxy");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("SmtpProxy");
            Console.WriteLine("Copyright (c) Douglas Associates 2011. All Rights Reserved.");
            Console.WriteLine();

            try
            {
                // Look for command line argument
                if (args.Contains("/console", StringComparer.OrdinalIgnoreCase))
                {
                    RunAsAConsole();
                }
                else
                {
                    // Default to running as a service
                    RunAsAService();
                }
            }
            catch (Exception ex)
            {
                Program.Trace.TraceEvent(TraceEventType.Critical, 0, "Unhandled Exception ({0}) caught:\n{1}", ex.Message, ex.StackTrace);
            }
        }
        static void RunAsAConsole()
        {
            using (new ProxyServer())
            {
                Console.WriteLine("SmtpProxy is now listening on port {0}.", Settings.Default.PortToListenOn);
                Console.WriteLine();
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
            }
        }
        static void RunAsAService()
        {
            Console.WriteLine("SmtpProxy is attempting to run as a Windows Service.");
            Console.WriteLine("To run as a console application, use the /console option.");
            Console.WriteLine();
            var ServicesToRun = new ServiceBase[] { new SmtpProxyService() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
