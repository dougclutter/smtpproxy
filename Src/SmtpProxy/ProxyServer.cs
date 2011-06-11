using System;
using System.Threading;
using System.Net.Sockets;
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
        readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        #region Constructor
        public ProxyServer()
        {
            Listener = new TcpListener(System.Net.IPAddress.Any, Settings.Default.PortToListenOn);
            Task.Factory.StartNew(() => Listen(TokenSource.Token), TokenSource.Token);
        }
        #endregion

        #region Properties
        public bool IsListening { get; private set; }
        #endregion

        #region Public Methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Protected / Private Methods
        void Listen(CancellationToken token)
        {
            Program.Trace.TraceInformation("Listen started");
            try
            {
                // Start the listener
                Listener.Start();
                IsListening = true;

                // Wait until a client tries to connect to the listener
                var state = new AcceptSocketState { Token = token };
                Task.Factory.StartNew(() => AcceptSocket(state), state.Token);

                // Run until cancelled
                token.WaitHandle.WaitOne();
            }
            finally
            {
                Program.Trace.TraceInformation("Listen ended");
                IsListening = false;
            }
        }
        void AcceptSocket(AcceptSocketState state)
        {
            Socket socket = null;
            try
            {
                // Wait until a client connects
                socket = Listener.AcceptSocket();

                // Make sure we weren't cancelled
                state.Token.ThrowIfCancellationRequested();

                // Process the socket on a new Task
                var processor = new SocketProcessor();
                Task.Factory.StartNew(() => processor.Start(socket, state.Token), state.Token);
            }
            catch (SocketException ex)
            {
                Program.Trace.TraceInformation("ProxyServer.AcceptSocket has ignored a SocketException ({0}) probably because the TcpListener was stopped.", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Listener not started or has been stopped
                Program.Trace.TraceInformation("ProxyServer.AcceptSocket has ignored an InvalidOperationException ({0}) probably because the TcpListener was stopped.", ex.Message);
            }
            finally
            {
                // Start listening for another client
                state.Token.ThrowIfCancellationRequested();
                Task.Factory.StartNew(() => AcceptSocket(state), state.Token);
            }
        }
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                TokenSource.Cancel();
                Listener.Stop();
            }
        }
        #endregion

        #region Internal Classes
        class AcceptSocketState
        {
            public CancellationToken Token;
        }
        #endregion
    }
}
