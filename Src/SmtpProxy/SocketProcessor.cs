/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Diagnostics;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace SmtpProxy
{
    /// <summary>
    /// Processes all the data sent to a given port by forwarding it to a SMTP server. Responses from the SMTP server are returned to the socket.
    /// </summary>
    class SocketProcessor
    {
        #region Constants
        const string EOL = "\r\n";
        #endregion

        #region Variables
        readonly Encoding Encoder = Encoding.ASCII;
        readonly byte[] SocketReceiveBuffer = new byte[512];
        readonly byte[] ServerReadBuffer = new byte[512];
        /// <summary>
        /// This event is set to true when either connection (email client or smtp server) is closed
        /// </summary>
        ManualResetEvent ConnectionClosed;
        #endregion

        #region Public Methods
        public void Start(Socket socket, CancellationToken token)
        {
            try
            {
                using (ConnectionClosed = new ManualResetEvent(false))
                using (var server = ConnectToServer(socket))
                {
                    // Begin listening for incoming traffic from the socket
                    Task.Factory.StartNew(() => SocketReceive(socket, server, token), token);

                    // Begin listening for incoming traffic from the smtp server
                    Task.Factory.StartNew(() => SmtpServerRead(socket, server, token), token);

                    // Keep waiting until cancelled or one of the connections is closed
                    WaitHandle.WaitAny(new WaitHandle[] { token.WaitHandle, ConnectionClosed });
                }
            }
            catch (ObjectDisposedException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.Start is ignoring an ObjectDisposedException ({0}) probably because a connection was closed.", ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.Start is ignoring an OperationCanceledException ({0}) probably because a connection was closed.", ex.Message);
            }
            catch (AggregateException ex)
            {
                Program.Trace.TraceEvent(System.Diagnostics.TraceEventType.Error, 2, "Unhandled exception ({0}) detected in Task", ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                Program.Trace.TraceEvent(TraceEventType.Error, 5,
                    "SocketProcessor.Start has thrown an unexpected Exception: {0}: {1}", ex.GetType().Name, ex.Message);
                throw;
            }
        }
        #endregion

        #region Protected / Private Methods
        SmtpServer ConnectToServer(Socket socket)
        {
            // Create a connection to the target server
            var server = new SmtpServer();

            // Send the connection message back through the socket
            var connectionMessage = Encoder.GetBytes(server.ConnectResponse + EOL);
            socket.Send(connectionMessage);

            return server;
        }
        void SocketReceive(Socket socket, SmtpServer server, CancellationToken token)
        {
            try
            {
                // Loop until cancellation is requested or the socket is closed.
                while (!token.IsCancellationRequested)
                {
                    // Block until the socket coughs up some data.
                    // If zero bytes returned, that means the caller has closed the socket gracefully.
                    var bytesRead = socket.Receive(SocketReceiveBuffer);
                    if (bytesRead == 0)
                    {
                        ConnectionClosed.Set();
                        return;
                    }

                    // Write to Debug
                    string inputString = Encoder.GetString(SocketReceiveBuffer, 0, bytesRead);
                    Program.Trace.TraceInformation(inputString);

                    // Write to server
                    server.Write(SocketReceiveBuffer, bytesRead);
                }
            }
            catch (SocketException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.SocketReceive is ignoring a SocketException ({0}).", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.SocketReceive is ignoring an ObjectDisposedException ({0}) probably because the Socket was closed.", ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                Program.Trace.TraceEvent(TraceEventType.Error, 3,
                    "SocketProcessor.SocketReceive has thrown an unexpected Exception: {0}: {1}", ex.GetType().Name, ex.Message);
                throw;
            }
        }
        void SmtpServerRead(Socket socket, SmtpServer server, CancellationToken token)
        {
            try
            {
                // Loop until cancellation is requested or the SMTP server closes the connection.
                while (!token.IsCancellationRequested)
                {
                    // Block until SMTP server coughs up some data.
                    // If zero bytes returned, that means the SMTP server has closed the connection gracefully.
                    var bytesRead = server.Read(ServerReadBuffer);
                    if (bytesRead == 0)
                    {
                        ConnectionClosed.Set();
                        return;
                    }

                    // Write to Debug
                    string outputString = Encoder.GetString(ServerReadBuffer, 0, bytesRead);
                    Program.Trace.TraceInformation(outputString);

                    // Write to socket
                    socket.Send(ServerReadBuffer, 0, bytesRead, SocketFlags.None);
                }
            }
            catch (IOException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.SmtpServerRead has ignored an IOException ({0}) probably because the SmtpServer was Disposed.", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.SmtpServerRead has ignored an ObjectDisposedException ({0}) probably because the SmtpServer was Disposed.", ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                Program.Trace.TraceEvent(TraceEventType.Error, 3,
                    "SocketProcessor.SmtpServerRead has thrown an unexpected Exception: {0}: {1}", ex.GetType().Name, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
