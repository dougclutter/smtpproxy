/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.ServiceProcess;

namespace SmtpProxy
{
    /// <summary>
    /// Starts and stops a ProxyServer as requested by Windows
    /// </summary>
    public partial class SmtpProxyService : ServiceBase
    {
        ProxyServer Server;

        public SmtpProxyService()
        {
            Program.Trace.TraceInformation("=============================================");
            Program.Trace.TraceInformation("SmtpProxyService Initializing");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.Trace.TraceInformation("SmtpProxyService.OnStart called");
            StopProxyServer();
            Server = new ProxyServer();
            Server.StartListening();
        }

        protected override void OnStop()
        {
            Program.Trace.TraceInformation("SmtpProxyService.OnStop called");
            StopProxyServer();
        }

        void StopProxyServer()
        {
            if (Server != null)
            {
                Program.Trace.TraceInformation("ProxyService is being Disposed");
                Server.Dispose();
                Server = null;
            }
        }
    }
}
