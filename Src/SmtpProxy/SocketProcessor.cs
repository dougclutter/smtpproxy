using System;
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
        ManualResetEvent SocketClosed;
        #endregion

        #region Public Methods
        public void Start(Socket socket, CancellationToken token)
        {
            Program.Trace.TraceInformation("Socket {0} opened", socket.Handle);
            try
            {
                using (SocketClosed = new ManualResetEvent(false))
                using (var server = ConnectToServer(socket))
                {
                    // Begin listening for incoming traffic from the socket
                    Task.Factory.StartNew(() => SocketReceive(socket, server, token), token);

                    // Begin listening for incoming traffic from the smtp server
                    Task.Factory.StartNew(() => SmtpServerRead(socket, server, token), token);

                    // Keep waiting until cancelled or the socket is closed by the sender
                    WaitHandle.WaitAny(new WaitHandle[] { token.WaitHandle, SocketClosed });
                }
            }
            catch (ObjectDisposedException ex)
            {
                Program.Trace.TraceInformation("SocketProcessor.Start is ignoring an ObjectDisposedException ({0}) probably because the Socket was closed.", ex.Message);
            }
            finally
            {
                // Close the socket
                if (socket != null)
                {
                    socket.Close();
                    Program.Trace.TraceInformation("Socket {0} closed", socket.Handle);
                }

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
                // This endless loop will be broken when cancellation is requested
                // or when the socket/server is closed.
                while (true)
                {
                    // Block until the socket coughs up some data
                    var bytesRead = socket.Receive(SocketReceiveBuffer);

                    // Bail out if we were cancelled
                    token.ThrowIfCancellationRequested();

                    // If zero bytes returned, that means the caller has closed the socket gracefully.
                    if (bytesRead == 0)
                    {
                        SocketClosed.Set();
                        return;
                    }

                    // Write to Debug
                    string inputString = Encoder.GetString(SocketReceiveBuffer, 0, bytesRead);
                    Program.Trace.TraceInformation(inputString);

                    // Write to server
                    server.Write(SocketReceiveBuffer, bytesRead);


                    // Again, bail out if we were cancelled
                    token.ThrowIfCancellationRequested();
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
        }
        void SmtpServerRead(Socket socket, SmtpServer server, CancellationToken token)
        {
            try
            {
                // This endless loop will be broken when cancellation is requested
                // or when the socket/server is closed.
                while (true)
                {
                    // Block until server coughs up some data
                    var bytesRead = server.Read(ServerReadBuffer);

                    // Bail out if we were cancelled
                    token.ThrowIfCancellationRequested();

                    // If zero bytes returned, that means the caller has closed the server gracefully.
                    if (bytesRead == 0)
                        return;

                    // Write to Debug
                    string outputString = Encoder.GetString(ServerReadBuffer, 0, bytesRead);
                    Program.Trace.TraceInformation(outputString);

                    // Write to socket
                    socket.Send(ServerReadBuffer, 0, bytesRead, SocketFlags.None);

                    // Again, bail out if we were cancelled
                    token.ThrowIfCancellationRequested();
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
        }
        #endregion
    }
}
