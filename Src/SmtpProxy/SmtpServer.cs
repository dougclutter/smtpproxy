/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using SmtpProxy.Properties;
using System.Diagnostics;

namespace SmtpProxy
{
    /// <summary>
    /// Connects to an SMTP server using an encrypted connection and then provides Read/Write methods to send/receive data from the SMTP server.
    /// </summary>
    class SmtpServer : IDisposable
    {
        #region Variables
        TcpClient Client;
        NetworkStream NetworkStream;
        SslStream SecureStream;
        StreamReader ClearTextReader;
        StreamWriter ClearTextWriter;
        #endregion

        #region Constructors
        public SmtpServer()
        {
            Initialize();
        }
        #endregion

        #region Properties
        public string ConnectResponse {get; private set;}
        #endregion

        #region Public Methods
        public void Write(byte[] buffer, int count)
        {
            SecureStream.Write(buffer, 0, count);
            SecureStream.Flush();
        }
        public int Read(byte[] buffer)
        {
            return SecureStream.Read(buffer, 0, buffer.Length);
        }
        public void Dispose()
        {
            Program.Trace.TraceInformation("Disconnecting from SMTP server {0}:{1}", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Protected / Private Methods
        void Initialize()
        {
            // In testing, I found that the smtp.live.com servers will sometimes fail to connect.
            // Retry the connection until it works or the timeout expires.
            var stopTime = DateTime.Now.AddMinutes(2);
            Exception lastException = null;
            while (DateTime.Now < stopTime)
            {
                try
                {
                    ConnectToServer();
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    lastException = ex;
                    Program.Trace.TraceInformation("SmtpServer.Initialize is ignoring an InvalidOperationException ({0}) probably due to an inability to connect to the SMTP server.", ex.Message);
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    Program.Trace.TraceInformation("SmtpServer.Initialize is ignoring an IOException ({0}) probably due to an inability to connect to the SMTP server.", ex.Message);
                }
                Program.Trace.TraceEvent(TraceEventType.Warning, 1, 
                    "Failed to connect to SMTP server {0}:{1}.", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

                // Release any resources that were allocated and sleep briefly before trying again
                ReleaseResources();
                System.Threading.Thread.Sleep(2500);
            }
            throw new TimeoutException("Unable to connect to SMTP server.", lastException);
        }
        void ConnectToServer()
        {
            Program.Trace.TraceInformation("Connecting to SMTP server {0}:{1}", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

            // Connect to the TargetServer
            Client = new TcpClient(Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);
            NetworkStream = Client.GetStream();

            // Create clear text reader/writer to help establish encrypted connection
            ClearTextReader = new StreamReader(NetworkStream);
            ClearTextWriter = new StreamWriter(NetworkStream) { AutoFlush = true };

            ConnectResponse = ClearTextReader.ReadLine();
            if (!ConnectResponse.StartsWith("220 "))
                throw new InvalidOperationException("SMTP Server did not respond to connection request");

            ClearTextWriter.WriteLine("HELO {0}", Environment.MachineName);
            var helloResponse = ClearTextReader.ReadLine();
            if (!helloResponse.StartsWith("250 "))
                throw new InvalidOperationException("SMTP Server did not respond to HELO request");

            ClearTextWriter.WriteLine("STARTTLS");
            var startTlsResponse = ClearTextReader.ReadLine();
            if (!startTlsResponse.StartsWith("220 "))
                throw new InvalidOperationException("SMTP Server did not respond to STARTTLS request");

            SecureStream = new SslStream(NetworkStream, false, null, null, EncryptionPolicy.RequireEncryption);
            SecureStream.AuthenticateAsClient(Settings.Default.SmtpHostUrl);
        }
        void ReleaseResources()
        {
            if ((ClearTextReader != null))
                ClearTextReader.Close();

            if (ClearTextWriter != null)
                ClearTextWriter.Close();

            if (SecureStream != null)
                SecureStream.Close();

            if (NetworkStream != null)
                NetworkStream.Close();

            if (Client != null)
                Client.Close();
        }
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseResources();
            }
        }
        #endregion
    }
}
