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
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StopProxyServer();
            Server = new ProxyServer();
        }

        protected override void OnStop()
        {
            StopProxyServer();
        }

        void StopProxyServer()
        {
            if (Server != null)
                Server.Dispose();
        }
    }
}
