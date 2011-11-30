/* Copyright (c) Douglas Associates 2011. All Rights Reserved. 
 * Licensed for use under the Microsoft Public License which is included by reference here.
 */
using System;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using SmtpProxy.Properties;
using System.Diagnostics;
using System.Threading;

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

        #region Public Methods
        public string Connect()
        {
            // In testing, I found that the smtp.live.com servers will sometimes fail to connect.
            // Retry the connection until it works or the timeout expires.
            var stopTime = DateTime.Now.AddMinutes(2);
            Exception lastException = null;
            while (DateTime.Now < stopTime)
            {
                try
                {
                    return ConnectToServer();
                }
                catch (InvalidOperationException ex)
                {
                    IgnoreException(lastException = ex);
                }
                catch (IOException ex)
                {
                    IgnoreException(lastException = ex);
                }
                Program.Trace.TraceEvent(TraceEventType.Warning, 1001,
                    "Failed to connect to SMTP server {0}:{1}.", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

                // Release any resources that were allocated and sleep briefly before trying again
                ReleaseResources();
                Thread.Sleep(2500);
            }
            throw new TimeoutException("Unable to connect to SMTP server.", lastException);
        }
        public void Write(byte[] buffer, int count)
        {
            ThrowIfClientNull();
            SecureStream.Write(buffer, 0, count);
            SecureStream.Flush();
        }
        public int Read(byte[] buffer)
        {
            ThrowIfClientNull();
            return SecureStream.Read(buffer, 0, buffer.Length);
        }
        public bool IsConnected { get { return Client.Client.IsConnected(); } }
        public int Available { get { return Client.Client.Available; } }
        public void Dispose()
        {
            Program.Trace.TraceEvent(TraceEventType.Information, 1022, "Disconnecting from SMTP server {0}:{1}", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Protected / Private Methods
        void ThrowIfClientNull()
        {
            if (Client == null)
                throw new InvalidOperationException("Cannot perform this operation until Connect has been called");
        }
        static void IgnoreException(Exception ex)
        {
            Program.Trace.TraceEvent(TraceEventType.Information, 1011,
                "SmtpServer is ignoring the following exception probably due to an inability to connect to the SMTP server: {0}: {1}", 
                ex.GetType().Name, ex.Message);
        }
        string ConnectToServer()
        {
            Program.Trace.TraceEvent(TraceEventType.Information, 1023, "Connecting to SMTP server {0}:{1}", Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);

            // Connect to the TargetServer
            Client = new TcpClient(Settings.Default.SmtpHostUrl, Settings.Default.SmtpPort);
            NetworkStream = Client.GetStream();

            // Create clear text reader/writer to help establish encrypted connection
            ClearTextReader = new StreamReader(NetworkStream);
            ClearTextWriter = new StreamWriter(NetworkStream) { AutoFlush = true };

            string response = ClearTextReader.ReadLine();
            if (!response.StartsWith("220 "))
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

            return response;
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
