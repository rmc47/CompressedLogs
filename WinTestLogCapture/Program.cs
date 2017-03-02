using CompressedLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinTestLogCapture
{
    class Program
    {
        private static IPEndPoint s_BroadcastEndpoint;
        private static IPEndPoint s_SourceEndpoint;
        private static Socket s_Socket;

        public static void Main()
        {
            QsoStore store = new QsoStore(ConfigurationSettings.AppSettings["DatabasePath"]);
            new System.Threading.Timer(_ => Program.UploadOutstandingQsos(), null, 0, 5 * 60 * 1000);

            s_Socket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
            s_Socket.ExclusiveAddressUse = false;
            s_Socket.EnableBroadcast = true;

            s_BroadcastEndpoint = new IPEndPoint(IPAddress.Parse(ConfigurationSettings.AppSettings["BroadcastAddress"]), 9871);
            s_SourceEndpoint = new IPEndPoint(IPAddress.Parse(ConfigurationSettings.AppSettings["SourceAddress"]), 9871);

            s_Socket.Bind(s_SourceEndpoint);

            Console.WriteLine("Ready to receive...");
            SendGab("Log capture running");
            List<byte[]> qsoBlock = new List<byte[]>();

            while (s_Socket.IsBound)
            {
                string rx = getUdpLine(s_Socket, s_BroadcastEndpoint);
                if (rx.Contains("ADDQSO:"))
                {
                    Console.WriteLine(rx);
                    Qso q = handleQso(rx);
                    if (!store.QsoExists(q))
                        store.AddQso(q);
                }
            }
            s_Socket.Close();
            //TODO: Rebind automatically if socket closes
        }

        private static void SendGab(string gabText)
        {
            SendUdpLine(s_Socket, s_BroadcastEndpoint, "GAB: \"LOGCAP\" \"\" \"" + gabText + "\"");
        }

        private static void UploadOutstandingQsos()
        {
            Console.WriteLine("==== Uploading QSOs ====");
            try
            {
                QsoStore store = new QsoStore(ConfigurationSettings.AppSettings["DatabasePath"]);
                List<Qso> qsos = store.GetUnprocessedQsos();
                if (qsos.Count == 0)
                    return;

                QsoCompressor compressor = new QsoCompressor();
                string url = ConfigurationSettings.AppSettings["LogUploadUrl"];
                HttpWebRequest req = HttpWebRequest.CreateHttp(url + "?qsoCount=" + qsos.Count + "&hash=something");
                req.Method = "POST";
                using (Stream reqStream = req.GetRequestStream())
                {
                    foreach (Qso q in qsos)
                    {
                        // Hack the QSO into the right epoch
                        q.QsoTime = new DateTime(QsoCompressor.s_DateTimeEpoch.Year, QsoCompressor.s_DateTimeEpoch.Month, q.QsoTime.Day, q.QsoTime.Hour, q.QsoTime.Minute, q.QsoTime.Second);
                        byte[] compressedQso = compressor.CompressQso(q);
                        reqStream.Write(compressedQso, 0, compressedQso.Length);
                    }
                }
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                {
                    string responseText = responseReader.ReadToEnd();
                    if (responseText.Contains("OK"))
                    {
                        foreach (Qso q in qsos)
                        {
                            store.MarkQsoProcessed(q);
                        }
                        SendGab("Uploaded " + qsos.Count + " QSOs");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error uploading: " + ex);
                SendGab("Error uploading QSOs");
            }
        }

        public static string getUdpLine(Socket sock, EndPoint ep)
        {
            byte[] data = new byte[1024];
            int recv = sock.ReceiveFrom(data, ref ep);

            byte checksum = 0;
            for (int i = 0; i < recv - 2; i++)
                checksum += data[i];
            checksum |= (byte)0x80;

            if (checksum != data[recv - 2])
                Debugger.Break();

            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            return stringData;
        }

        public static void SendUdpLine(Socket sock, EndPoint ep, string line)
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
