using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class HelperConnect
    {
        public IPEndPoint IP { get; set; }
        public Socket Client { get; set; }

        public HelperConnect()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }
    }
}
