using CompressedLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CompressedLogReceiver.Controllers
{
    public class LogsController : Controller
    {
        public ActionResult Index()
        {
            return Content("Hello world");
        }

        public ActionResult Submit(int qsoCount, string hash)
        {
            try
            {
                if (qsoCount < 1)
                    return new HttpStatusCodeResult(400, "Invalid QSO count");
                if (string.IsNullOrWhiteSpace(hash))
                    return new HttpStatusCodeResult(400, "Missing hash");

                int length = Request.ContentLength;
                byte[] incomingData = new byte[length];
                using (Stream requestStream = Request.InputStream)
                {
                    int pos = 0;
                    while (pos < length)
                    {
                        pos += requestStream.Read(incomingData, pos, length - pos);
                    }
                }

                List<Qso> submittedQsos = new List<Qso>(qsoCount);
                QsoCompressor compressor = new QsoCompressor();
                int decompressPos = 0;
                for (int i = 0; i < qsoCount; i++)
                {
                    submittedQsos.Add(compressor.UncompressQso(incomingData, ref decompressPos));
                }
                if (decompressPos != length)
                    return new HttpStatusCodeResult(400, "Length does not match expected length");

                QsoStore store = new QsoStore(ConfigurationSettings.AppSettings["DatabasePath"]);
                foreach (Qso q in submittedQsos)
                {
                    if (!store.QsoExists(q))
                    {
                        store.AddQso(q);
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
            return Content("OK");
        }
    }
}
