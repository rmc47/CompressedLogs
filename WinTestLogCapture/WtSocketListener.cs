using CompressedLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinTestLogCapture
{
    class WtSocketListener
    {
        private Socket m_Socket;
        private IPEndPoint m_BroadcastEndpoint;
        private QsoStore m_QsoStore;

        private volatile bool m_Listening;
        private volatile bool m_CloseRequest;

        public WtSocketListener(IPEndPoint sourceEndpoint, IPEndPoint broadcastEndpoint)
        {
            m_QsoStore = new QsoStore(Settings.DatabasePath);

            m_Socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            m_Socket.ExclusiveAddressUse = false;
            m_Socket.EnableBroadcast = true;

            m_Socket.Bind(sourceEndpoint);
            m_Socket.ReceiveTimeout = 5000;

            m_BroadcastEndpoint = broadcastEndpoint;
        }
        
        public bool Listening { get { return m_Listening; } }

        public void StartListening()
        {
            new Thread(_ => ListenWorker()).Start();
        }

        public void Close()
        {
            m_CloseRequest = true;
        }

        private void ListenWorker()
        {
            try
            {
                m_Listening = true;
                while (m_Socket.IsBound && !m_CloseRequest)
                {
                    string rx = getUdpLine(m_Socket, m_BroadcastEndpoint);
                    if (rx.Contains("ADDQSO:"))
                    {
                        Console.WriteLine(rx);
                        Qso q = handleQso(rx);
                        if (!m_QsoStore.QsoExists(q))
                            m_QsoStore.AddQso(q);
                    }
                }
                m_Socket.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("Socket closed: {0}", m_BroadcastEndpoint);
                m_Listening = false;
            }
        }

        private void SendGab(string gabText)
        {
            SendUdpLine(m_Socket, m_BroadcastEndpoint, "GAB: \"LOGCAP\" \"\" \"" + gabText + "\"");
        }

        public string getUdpLine(Socket sock, EndPoint ep)
        {
            byte[] data = new byte[1024];
            int recv;
            try
            {
                recv = sock.ReceiveFrom(data, ref ep);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                    return string.Empty;
                else
                    throw;
            }

            byte checksum = 0;
            for (int i = 0; i < recv - 2; i++)
                checksum += data[i];
            checksum |= (byte)0x80;
            
            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            return stringData;
        }

        public void SendUdpLine(Socket sock, EndPoint ep, string line)
        {
            byte[] data = Encoding.ASCII.GetBytes(line);

            // Win-Test's slightly crazy mod127 checksum
            byte checksum = 0;
            for (int i = 0; i < data.Length; i++)
                checksum += data[i];
            checksum |= 0x80;

            byte[] dataWithChecksum = new byte[data.Length + 2];
            Buffer.BlockCopy(data, 0, dataWithChecksum, 0, data.Length);
            dataWithChecksum[data.Length] = checksum;
            dataWithChecksum[data.Length + 1] = 0;

            sock.SendTo(dataWithChecksum, ep);
        }

        private static Qso handleQso(string rx)
        {
            Qso qso = new Qso();
            string[] values = rx.Split(' ');
            qso.Band = ParseWtBand(int.Parse(values[7]));
            qso.Mode = ParseWtMode(int.Parse(values[6]));
            qso.QsoTime = ParseWtTime(long.Parse(values[4]));
            qso.Callsign = values[13].Replace("\"", "");
            qso.Operator = values[22].Replace("\"", "");
            return qso;
        }

        private static Band ParseWtBand(int bandId)
        {
            switch (bandId)
            {
                case 1: return Band.B160m;
                case 2: return Band.B80m;
                case 3: return Band.B40m;
                case 4: return Band.B30m;
                case 5: return Band.B20m;
                case 6: return Band.B17m;
                case 7: return Band.B15m;
                case 8: return Band.B12m;
                case 9: return Band.B10m;
                case 10: return Band.B6m;
                default: return Band.Unknown;
            }
        }

        private static Mode ParseWtMode(int modeId)
        {
            switch (modeId)
            {
                case 0: return Mode.CW;
                case 1: return Mode.Phone;
                case 2: return Mode.Data;
                case 3: return Mode.Phone;
                case 4: return Mode.Data;
                default: return Mode.Unknown;
            }
        }

        private static DateTime ParseWtTime(long wtTimestamp)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return epoch.AddSeconds(wtTimestamp);
        }
    }
}
