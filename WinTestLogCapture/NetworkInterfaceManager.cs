using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WinTestLogCapture
{
    internal sealed class NetworkInterfaceManager
    {
        private Dictionary<IPAddress, WtSocketListener> m_SocketListeners = new Dictionary<IPAddress, WtSocketListener>();

        public void CheckSocketListeners()
        {
            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
            foreach (var unicastAddress in ipProps.GetUnicastAddresses())
            {
                if (unicastAddress.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    continue;
                if (unicastAddress.SuffixOrigin == SuffixOrigin.LinkLayerAddress)
                    continue;

                IPAddress address = unicastAddress.Address;
                byte[] broadcastAddressBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                    broadcastAddressBytes[i] = (byte)(address.GetAddressBytes()[i] | (byte)~unicastAddress.IPv4Mask.GetAddressBytes()[i]);
                IPAddress broadcastAddress = new IPAddress(broadcastAddressBytes);

                Console.WriteLine("Got address {0} with broadcast {1}", address, broadcastAddress);
                WtSocketListener listener;
                if (!m_SocketListeners.TryGetValue(address, out listener) || !listener.Listening)
                {
                    // Either we don't have a listener here, or it's broken itself (e.g. network interface went away and came back)
                    Console.WriteLine("Starting new socket listener for {0}", address);
                    listener = new WtSocketListener(new IPEndPoint(address, 9871), new IPEndPoint(broadcastAddress, 9871));
                    m_SocketListeners[address] = listener;
                    listener.StartListening();
                }
            }
        }

        public void CloseSocketListeners()
        {
            foreach (WtSocketListener listener in m_SocketListeners.Values)
                listener.Close();
        }
    }
}
