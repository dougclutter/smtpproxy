using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SmtpProxy
{
    // Thanks to http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
    static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
    }
}
