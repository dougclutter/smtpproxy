/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Diagnostics;
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
            Program.Trace.TraceEvent(TraceEventType.Information, 1017, "=============================================");
            Program.Trace.TraceEvent(TraceEventType.Information, 1018, "SmtpProxyService Initializing");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.Trace.TraceEvent(TraceEventType.Information, 1019, "SmtpProxyService.OnStart called");
            StopProxyServer();
            Server = new ProxyServer();
            Server.StartListening();
        }

        protected override void OnStop()
        {
            Program.Trace.TraceEvent(TraceEventType.Information, 1020, "SmtpProxyService.OnStop called");
            StopProxyServer();
        }

        void StopProxyServer()
        {
            if (Server != null)
            {
                Program.Trace.TraceEvent(TraceEventType.Information, 1021, "ProxyService is being Disposed");
                Server.StopListening();
                Server.Dispose();
                Server = null;
            }
        }
    }
}
