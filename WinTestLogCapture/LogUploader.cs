using CompressedLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WinTestLogCapture
{
    internal sealed class LogUploader
    {
        public static void UploadOutstandingQsos()
        {
            Console.WriteLine("==== Uploading QSOs ====");
            try
            {
                QsoStore store = new QsoStore(Settings.DatabasePath);
                List<Qso> qsos;
                while (true)
                {
                    qsos = store.GetUnprocessedQsos();
                    if (qsos.Count == 0)
                        break;
                    if (qsos.Count > 20)
                        qsos = qsos.Take(20).ToList();

                    QsoCompressor compressor = new QsoCompressor();
                    string url = Settings.LogUploadUrl;
                    HttpWebRequest req = HttpWebRequest.CreateHttp(url + "?qsoCount=" + qsos.Count + "&hash=something");
                    req.Method = "POST";
                    req.KeepAlive = false;
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
                            //SendGab("Uploaded " + qsos.Count + " QSOs");
                        }
                        else
                        {
                            Console.WriteLine("Error from server: " + responseText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error uploading: " + ex);
                //SendGab("Error uploading QSOs");
            }
        }
    }
}
