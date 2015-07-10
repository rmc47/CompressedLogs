using CompressedLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLogTests
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void SubmitMediumLog()
        {
            string adifText = File.ReadAllText("mediumlog.adi");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoCompressor compressor = new QsoCompressor ();

            HttpWebRequest req = HttpWebRequest.CreateHttp("http://localhost:55950/logs/submit?qsoCount=" + qsos.Count + "&hash=something");
            req.Method = "POST";
            using (Stream reqStream = req.GetRequestStream())
            {
                foreach (Qso q in qsos)
                {
                    // Hack the QSO into the right epoch
                    q.QsoTime = new DateTime(2015, 07, q.QsoTime.Day, q.QsoTime.Hour, q.QsoTime.Minute, q.QsoTime.Second);
                    byte[] compressedQso = compressor.CompressQso(q);
                    reqStream.Write(compressedQso, 0, compressedQso.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)req.GetResponse();
            using (StreamReader responseReader = new StreamReader (response.GetResponseStream()))
            {
                string responseText = responseReader.ReadToEnd();
                Assert.AreEqual("OK", responseText.Trim());
            }
        }
    }
}
