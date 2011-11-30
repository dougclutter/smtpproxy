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
            ConsoleAndTraceWriteLine("Starting up at {0:g}.", DateTime.Now);
            ConsoleAndTraceWriteLine("Settings have been read from {0}", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            ConsoleAndTraceWriteLine("Listening on port {0}.", Settings.Default.PortToListenOn);
            ConsoleAndTraceWriteLine("Traffic will be forwarded to {0}:{1}", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);
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
                Program.Trace.TraceEvent(TraceEventType.Critical, 1004, "Unhandled Exception caught:\n{0}: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            }
        }
        static void RunAsAConsole()
        {
            using (var server = new ProxyServer())
            {
                server.StartListening();
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
                server.StopListening();
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
        static void ConsoleAndTraceWriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Trace.TraceEvent(TraceEventType.Information, 1000, format, args);
        }
    }
}
