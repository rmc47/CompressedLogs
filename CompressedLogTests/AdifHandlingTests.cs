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
    public class AdifHandlingTests
    {
        [Test]
        public void BasicExportToAdif()
        {
            Qso source = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime(2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };

            string adif = AdifHandler.ExportContacts(new List<Qso> { source });
        }

        [Test]
        public void BasicRoundtripAdif()
        {
            Qso source = new Qso
            {
                Callsign = "JW1ABC",
                QsoTime = new DateTime(2015, 07, 13, 15, 17, 0),
                Band = Band.B40m,
                Mode = Mode.CW,
                Operator = "M0VFC",
            };

            string adif = AdifHandler.ExportContacts(new List<Qso> { source });

            List<Qso> qsos = AdifHandler.ImportAdif(adif);

            Assert.AreEqual(1, qsos.Count);

        }
    }
}
