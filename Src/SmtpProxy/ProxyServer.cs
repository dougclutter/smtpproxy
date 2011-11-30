/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SmtpProxy.Properties;

namespace SmtpProxy
{
    /// <summary>
    /// Listens for traffic on a port and forwards that traffic to an SMTP server
    /// </summary>
    public class ProxyServer : IDisposable
    {
        #region Variables
        readonly TcpListener Listener = new TcpListener(System.Net.IPAddress.Any, Settings.Default.PortToListenOn);
        readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        #region Public Methods
        public void StartListening()
        {
            // Start on a new thread so StartListening can return immediately
            Task.Factory.StartNew(() => Listen(), TokenSource.Token);
        }
        public void StopListening()
        {
            // Cancel TokenSource so child tasks will exit gracefully
            TokenSource.Cancel();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Protected / Private Methods
        void Listen()
        {
            // Start the listener
            Listener.Start();
            Program.Trace.TraceEvent(TraceEventType.Information, 1005, "Listener started. Connections on port {0} will be forwarded to {1}:{2}",
                Settings.Default.PortToListenOn,
                Settings.Default.SmtpHostUrl,
                Settings.Default.SmtpPort);

            // Pause for 100ms or exit if cancelled.
            while (!TokenSource.Token.WaitHandle.WaitOne(100))
            {
                // Check for a pending connection and process it if found
                if (Listener.Pending())
                {
                    // Grab the pending connection.
                    // The socket will be closed in ProcessTraffic.
                    Socket socket = Listener.AcceptSocket();
                    Program.Trace.TraceEvent(TraceEventType.Information, 1013, "Socket {0} opened", socket.Handle);

                    // Process the socket request in a new task so we can get the next request before the current one is done processing
                    Task.Factory.StartNew(() => ProcessTraffic(socket), TokenSource.Token);
                }
            }

            // Stop the listener so no further input will be received and processed
            Listener.Stop();
            Program.Trace.TraceEvent(TraceEventType.Information, 1014, "Listener on port {0} stopped ", Settings.Default.PortToListenOn);
        }
        void ProcessTraffic(Socket socket)
        {
            try
            {
                // If a cancellation was requested, then we need to exit.
                // The finally clause ensures the socket is shutdown and closed.
                TokenSource.Token.ThrowIfCancellationRequested();

                // Process the socket
                var processor = new TrafficProcessor(socket, TokenSource.Token);
                processor.Process();
            }
            catch (SocketException ex)
            {
                IgnoreException(ex);
            }
            catch (InvalidOperationException ex)
            {
                IgnoreException(ex);
            }
            catch (IOException ex)
            {
                IgnoreException(ex);
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                Program.Trace.TraceEvent(TraceEventType.Error, 1003,
                    "ProxyServer.ProcessTraffic has thrown an unexpected Exception: {0}: {1}", ex.GetType().Name, ex.Message);
                throw;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    Program.Trace.TraceEvent(TraceEventType.Information, 1015, "Socket {0} closed", socket.Handle);
                }
            }
        }
        static void IgnoreException(Exception ex)
        {
            Program.Trace.TraceEvent(TraceEventType.Warning, 1010,
                "ProxyServer.ProcessTraffic has ignored an {0}: {1}", ex.GetType().Name, ex.Message);

        }
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Force the listening to stop
                StopListening();

                TokenSource.Dispose();
                Listener.Stop();
                Program.Trace.TraceEvent(TraceEventType.Information, 1016,"ProxyServer has been Disposed");
            }
        }
        #endregion
    }
}
