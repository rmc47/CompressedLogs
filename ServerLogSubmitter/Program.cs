using CompressedLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLogSubmitter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world.");
            QsoStore store = new QsoStore();
            List<Qso> unprocessedQsos = store.GetUnprocessedQsos();
            
            // Split unprocessed QSOs by operator, then spit out to ADIF, push to TQSL and Club Log
            foreach (var operatorQsos in unprocessedQsos.GroupBy(q => q.Operator))
            {
                string op = operatorQsos.Key;
                List<Qso> qs = operatorQsos.ToList();
                string adif = AdifHandler.ExportContacts(qs);
                string adifForExport = Path.Combine("C:\\CompressedLog", "adifs", op, DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss"));
                string exportFolder = Path.GetDirectoryName(adifForExport);
                if (!Directory.Exists(exportFolder))
                    Directory.CreateDirectory(exportFolder);

                // We shouldn't ever have an existing file, but just in case
                while (File.Exists(adifForExport + ".adi"))
                    adifForExport += "-1";
                adifForExport += ".adi";

                // Dump the ADIF out to it
                File.WriteAllText(adifForExport, adif);

                // Upload to LoTW
                // TOOD: pass through location properly, not just guessed from op
                SubmitAdifToLotw(adifForExport, op);
                
                // TODO: submit to Club Log

                foreach (Qso q in qs)
                    store.MarkQsoProcessed(q);
            }
        }

        private static void SubmitAdifToLotw(string adifPath, string op)
        {
            string tqslOptions = string.Format("-a all -d -l \"{0}\" -q -u \"{1}\"", "JW/" + op, adifPath);
            ProcessStartInfo psi = new ProcessStartInfo("c:\\Program Files (x86)\\TrustedQSL\\tqsl.exe", tqslOptions);
            Process p = new Process ();
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
        }
    }
}
