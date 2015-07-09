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
    public class QsoCompressorTests
    {
        [Test]
        public void BasicQsoCompression()
        {
            Qso source = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime (2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };
            byte[] compressedQso = new QsoCompressor().CompressQso(source);
            Qso output = new QsoCompressor().UncompressQso(compressedQso, 0);

            AssertQsosEqual(source, output);
        }

        [Test]
        public void TwoQsoCompression()
        {
            Qso source1 = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime(2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };
            byte[] compressedQso1 = new QsoCompressor().CompressQso(source1);
            Qso source2 = new Qso
            {
                Callsign = "K3LR",
                QsoTime = new DateTime(2015, 07, 18, 3, 1, 0),
                Band = Band.B12m,
                Mode = Mode.Phone,
                Operator = "G3ZAY",
            };
            byte[] compressedQso2 = new QsoCompressor().CompressQso(source2);

            byte[] bothQsos = new byte[compressedQso1.Length + compressedQso2.Length];
            Buffer.BlockCopy(compressedQso1, 0, bothQsos, 0, compressedQso1.Length);
            Buffer.BlockCopy(compressedQso2, 0, bothQsos, compressedQso1.Length, compressedQso2.Length);

            int decompressPosition = 0;
            Qso output1 = new QsoCompressor().UncompressQso(bothQsos, ref decompressPosition);
            Qso output2 = new QsoCompressor().UncompressQso(bothQsos, ref decompressPosition);

            AssertQsosEqual(source1, output1);
            AssertQsosEqual(source2, output2);
        }

        [Test, Explicit]
        public void WebServiceSubmission()
        {
            Qso source1 = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime(2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };
            byte[] compressedQso1 = new QsoCompressor().CompressQso(source1);

            HttpWebRequest req = HttpWebRequest.CreateHttp("http://localhost:55950/logs/submit?qsoCount=1&hash=something");
            req.Method = "POST";
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(compressedQso1, 0, compressedQso1.Length);
            }
            req.GetResponse();
        }

        private void AssertQsosEqual(Qso source, Qso target)
        {
            Assert.AreEqual(source.Band, target.Band);
            Assert.AreEqual(source.Callsign, target.Callsign);
            Assert.AreEqual(source.Mode, target.Mode);
            Assert.AreEqual(source.Operator, target.Operator);
            Assert.AreEqual(source.QsoTime, target.QsoTime);
        }
    }
}
