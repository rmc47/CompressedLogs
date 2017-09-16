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
                    q.QsoTime = new DateTime(QsoCompressor.s_DateTimeEpoch.Year, QsoCompressor.s_DateTimeEpoch.Month, q.QsoTime.Day, q.QsoTime.Hour, q.QsoTime.Minute, q.QsoTime.Second);
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

        [Test]
        public void SubmitJWLog()
        {
            string adifText = File.ReadAllText(@"C:\Users\rob\Documents\win-test\DXPED-HF-ALL_2015@JW_G6UW\DXPED-HF-ALL_2015_K3A@JW_G6UW.ADI");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoCompressor compressor = new QsoCompressor();

            HttpWebRequest req = HttpWebRequest.CreateHttp("http://platinum.syxis.co.uk:8105/logs/submit?qsoCount=" + qsos.Count + "&hash=something");
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
                Assert.AreEqual("OK", responseText.Trim());
            }
        }

        [Test]
        [Explicit]
        public void ImportFinalJWLog()
        {
            string adifText = File.ReadAllText(@"D:\dropbox\logs\JW2015\DXPED-HF-ALL_2015_K3A@JW_G6UW.ADI");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoStore store = new QsoStore("C:\\CompressedLogs\\test-qsodb.sqlite");
            QsoCompressor compressor = new QsoCompressor();
            foreach (Qso q in qsos)
            {
                Qso q2 = compressor.UncompressQso(compressor.CompressQso(q), 0);
                if (!store.QsoExists(q))
                    store.AddQso(q);
            }
        }

        [Test]
        [Explicit]
        public void ImportTFLog()
        {
            string adifText = File.ReadAllText(@"c:\users\rob\Documents\win-test\2016-09-CUWS-TF\2016-09-CUWS-TF.ADI");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoStore store = new QsoStore("C:\\CompressedLogs\\test-qsodb.sqlite");
            QsoCompressor compressor = new QsoCompressor();
            foreach (Qso q in qsos)
            {
                Qso q2 = compressor.UncompressQso(compressor.CompressQso(q), 0);
                if (!store.QsoExists(q))
                    store.AddQso(q);
            }
        }

        [Test]
        [Explicit]
        public void ImportC6Log()
        {
            string adifText = File.ReadAllText(@"C:\Users\rob\Documents\win-test\DXPED-HF-ALL_2017@C6APY\C6ACP.ADI");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoStore store = new QsoStore("C:\\CompressedLogs\\client-qsodb.sqlite");
            QsoCompressor compressor = new QsoCompressor();
            foreach (Qso q in qsos)
            {
                Qso q2 = compressor.UncompressQso(compressor.CompressQso(q), 0);
                if (!store.QsoExists(q))
                    store.AddQso(q);
            }
        }

        [Test]
        [Explicit]
        public void ImportC6FinalLog()
        {
            string adifText = File.ReadAllText(@"D:\dropbox\C6A-2017\2017-03-10-endops-DXPED-HF-ALL_2017_ACB@C6APY.ADI");
            List<Qso> qsos = AdifHandler.ImportAdif(adifText);
            QsoStore store = new QsoStore("C:\\CompressedLogs\\client-qsodb.sqlite");
            QsoCompressor compressor = new QsoCompressor();
            foreach (Qso q in qsos)
            {
                Qso q2 = compressor.UncompressQso(compressor.CompressQso(q), 0);
                if (!store.QsoExists(q))
                    store.AddQso(q);
            }
        }
    }
}
