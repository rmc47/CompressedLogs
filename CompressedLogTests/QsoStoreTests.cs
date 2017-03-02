using CompressedLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLogTests
{
    [TestFixture]
    public class QsoStoreTests
    {
        [Test]
        public void AddQso()
        {
            Qso source = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime (2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };

            var store = new QsoStore("C:\\CompressedLogs\\test-qsodb.sqlite");
            store.AddQso(source);
        }

        [Test]
        public void ProcessedTest()
        {
            Qso source = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime(2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };

            var store = new QsoStore("C:\\CompressedLogs\\test-qsodb.sqlite");
            if (store.QsoExists(source))
                store.DeleteQso(source);

            Assert.IsFalse(store.QsoExists(source));
            store.AddQso(source);
            Assert.IsTrue(store.QsoExists(source));
            int unprocessedCount = store.GetUnprocessedQsos().Count;
            store.MarkQsoProcessed(source);
            int unprocessedCountAfterProcessing = store.GetUnprocessedQsos().Count;
            Assert.AreEqual(unprocessedCount - 1, unprocessedCountAfterProcessing, "Expected unprocessed count to go down after processing");
        }
    }
}
