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
    class TrafficProcessor
    {
        #region Constants
        const string EOL = "\r\n";
        #endregion

        #region Variables
        readonly Socket ClientSocket;
        readonly CancellationToken CancelToken;
        readonly Encoding Encoder = Encoding.ASCII;
        readonly byte[] SocketReceiveBuffer = new byte[512];
        readonly byte[] ServerReadBuffer = new byte[512];
        SmtpServer SMTPServer;
        #endregion

        #region Constructor
        public TrafficProcessor(Socket clientSocket, CancellationToken token)
        {
            ClientSocket = clientSocket;
            CancelToken = token;
        }
        #endregion

        #region Public Methods
        public void Process()
        {
            // Create a connection to the target SMTP server
            using (SMTPServer = new SmtpServer())
            {
                // Connect to the SMTP server and send the connection response back through the socket
                var connectionResponse = SMTPServer.Connect();
                Program.Trace.TraceEvent(TraceEventType.Information, 1012, "SMTP server sent: " + connectionResponse);
                ClientSocket.Send(Encoder.GetBytes(connectionResponse + EOL));

                // Loop until cancelled or completed
                while (ClientSocket.IsConnected() && SMTPServer.IsConnected)
                {
                    // Exit if cancelled
                    CancelToken.ThrowIfCancellationRequested();

                    // If the client has sent data, read it and forward it to the SMTP server
                    ReadFromClient();

                    // If the SMTP server sent data, read it and forward it to the client
                    ReadFromSmtpServer();
                }
            }
        }
        #endregion

        #region Protected / Private Methods
        void ReadFromClient()
        {
            while (ClientSocket.IsConnected() && ClientSocket.Available > 0)
            {
                var bytesRead = ClientSocket.Receive(SocketReceiveBuffer);

                // Write to trace
                string inputString = Encoder.GetString(SocketReceiveBuffer, 0, bytesRead);
                Program.Trace.TraceEvent(TraceEventType.Information, 1006, "Client sent: " + inputString);

                // Write to server
                SMTPServer.Write(SocketReceiveBuffer, bytesRead);
            }
        }
        void ReadFromSmtpServer()
        {
            while (SMTPServer.IsConnected && SMTPServer.Available > 0)
            {
                var bytesRead = SMTPServer.Read(ServerReadBuffer);

                // Write to trace
                string outputString = Encoder.GetString(ServerReadBuffer, 0, bytesRead);
                Program.Trace.TraceEvent(TraceEventType.Information, 1007, "SMTP server sent: " + outputString);

                // Write to socket
                ClientSocket.Send(ServerReadBuffer, 0, bytesRead, SocketFlags.None);
            }
        }
        #endregion
    }
}
