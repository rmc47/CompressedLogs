using CompressedLog;
using System;
using System.Collections.Generic;
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
        public static void Main()
        {
            QsoStore store = new QsoStore();
            new System.Threading.Timer(_ => Program.UploadOutstandingQsos(), null, 0, 5 * 60 * 1000);
            Socket sock = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
            sock.ExclusiveAddressUse = false;
            sock.EnableBroadcast = true;
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("192.168.137.1"), 9871);
            sock.Bind(iep);
            EndPoint ep = (EndPoint)iep;
            Console.WriteLine("Ready to receive...");
            List<byte[]> qsoBlock = new List<byte[]>();

            while (sock.IsBound)
            {
                string rx = getUdpLine(sock, ep);
                if (rx.Contains("ADDQSO:"))
                {
                    Console.WriteLine(rx);
                    Qso q = handleQso(rx);
                    if (!store.QsoExists(q))
                        store.AddQso(q);
                }
            }
            sock.Close();
            //TODO: Rebind automatically if socket closes
        }

        private static void UploadOutstandingQsos()
        {
            Console.WriteLine("==== Uploading QSOs ====");
            try
            {
                QsoStore store = new QsoStore();
                List<Qso> qsos = store.GetUnprocessedQsos();
                if (qsos.Count == 0)
                    return;

                QsoCompressor compressor = new QsoCompressor();
                HttpWebRequest req = HttpWebRequest.CreateHttp("http://51.255.135.163:8080/logs/submit?qsoCount=" + qsos.Count + "&hash=something");
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error uploading: " + ex);
            }
        }

        public static string getUdpLine(Socket sock, EndPoint ep)
        {
            byte[] data = new byte[1024];
            int recv = sock.ReceiveFrom(data, ref ep);
            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            return stringData;
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
