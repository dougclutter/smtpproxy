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
        readonly TcpListener Listener;
        readonly CancellationTokenSource TokenSource;
        bool IsDisposed;
        #endregion
        #region Constructor
        public ProxyServer()
        {
            // Initialize cancellation token and listener
            TokenSource = new CancellationTokenSource();
            Listener = new TcpListener(System.Net.IPAddress.Any, Settings.Default.PortToListenOn);
        }
        #endregion
        
        #region Public Methods
        public void StartListening()
        {
            if (IsDisposed)
                throw new InvalidOperationException("ProxyServer.StartListening() cannot be called after the object has been Disposed or StopListening has been called.");

            // Start on a new thread so StartListening can return immediately
            Task.Factory.StartNew(() => Listen());
        }
        public void StopListening()
        {
            Dispose();
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
            Program.Trace.TraceInformation("Listener started. Connections on port {0} will be forwarded to {1}:{2}", 
                Settings.Default.PortToListenOn,
                Settings.Default.SmtpHostUrl,
                Settings.Default.SmtpPort);

            // Loop until cancellation is requested
            while (!TokenSource.Token.IsCancellationRequested)
            {
                AcceptSocket();
            }
        }
        void AcceptSocket()
        {
            try
            {
                // Sleep until a request is pending OR the request has been cancelled
                while (!Listener.Pending() && !TokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(500);
                }

                // If we have a pending request AND we have NOT been cancelled,
                // process the pending request in a new Task.
                if (Listener.Pending() && !TokenSource.Token.IsCancellationRequested)
                {
                    // Accept the pending connection.
                    // The socket will be closed in ProcessSocket.
                    Socket socket = Listener.AcceptSocket();

                    // Process the socket request in a new task so we can start waiting for the next request before the current is done processing
                    Task.Factory.StartNew(() => ProcessSocket(socket));
                }
            }
            catch (InvalidOperationException ex)
            {
                // Listener not started or has been stopped
                Program.Trace.TraceInformation("ProxyServer.AcceptSocket has ignored an InvalidOperationException ({0}).", ex.Message);
            }
        }
        void ProcessSocket(Socket socket)
        {
            try
            {
                // Process the socket
                var processor = new SocketProcessor();
                processor.Start(socket, TokenSource.Token);
            }
            catch (SocketException ex)
            {
                // Listener may have been stopped
                Program.Trace.TraceInformation("ProxyServer.ProcessSocket has ignored a SocketException ({0}).", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Listener not started or has been stopped
                Program.Trace.TraceInformation("ProxyServer.ProcessSocket has ignored an InvalidOperationException ({0}).", ex.Message);
            }
            catch (IOException ex)
            {
                // Failed to complete a read operation
                Program.Trace.TraceInformation("ProxyServer.ProcessSocket has ignored an IOException ({0}).", ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                Program.Trace.TraceEvent(TraceEventType.Error, 3,
                    "ProxyServer.ProcessSocket has thrown an unexpected Exception: {0}: {1}", ex.GetType().Name, ex.Message);
                throw;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Close();
                    Program.Trace.TraceInformation("Socket {0} closed", socket.Handle);
                }
            }
        }
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;

                    // Cancel TokenSource so child tasks will exit gracefully
                    TokenSource.Cancel();

                    // Stop the listener so no further input will be received and processed
                    Listener.Stop();
                    Program.Trace.TraceInformation("Listener on port {0} stopped ", Settings.Default.PortToListenOn);
                }
            }
        }
        #endregion
    }
}
