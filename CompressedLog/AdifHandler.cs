using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CompressedLog
{
    public static class AdifHandler
    {
        public static string ExportContacts(IEnumerable<Qso> contacts)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ADIF export from M0VFC CompressedLog");
            sb.AppendLine("<EOH>");
            sb.AppendLine();

            foreach (Qso c in contacts)
                ExportContact(c, sb);

            return sb.ToString();

        }

        private static void ExportContact(Qso contact, StringBuilder writer)
        {
            WriteField("call", contact.Callsign, writer);
            WriteField("qso_date", contact.QsoTime.ToString("yyyyMMdd"), writer);
            WriteField("time_on", contact.QsoTime.ToString("HHmm"), writer);
            WriteField("mode", ModeText(contact.Mode), writer);
            WriteField("band", BandText(contact.Band), writer);
            WriteField("rst_sent", RstFromMode(contact.Mode), writer);
            WriteField("rst_rcvd", RstFromMode(contact.Mode), writer);
            writer.AppendLine("<EOR>");
            writer.AppendLine();
        }

        private static string ModeText(Mode m)
        {
            switch (m)
            {
                case Mode.Phone:
                    return "SSB";
                case Mode.CW:
                    return "CW";
                case Mode.Data:
                    return "RTTY";
                default:
                    return "UNKNOWN";
            }
        }

        private static string BandText(Band b)
        {
            // TODO Oh, such a hack :-)
            return b.ToString().Substring(1);
        }

        private static string RstFromMode(Mode m)
        {
            switch (m)
            {
                case Mode.Phone:
                    return "59";
                default:
                    return "599";
            }
        }

        private static void WriteField(string fieldName, string val, StringBuilder writer)
        {
            writer.AppendFormat("<{0}:{1}>{2}", fieldName.ToUpper(), val.Length, val.ToUpper() + " ");
        }

        public static List<Qso> ImportAdif(string adifText)
        {
            AdifFileReader adifReader = AdifFileReader.LoadFromContent(adifText);

            //int offset = 0;
            AdifFileReader.Header header = adifReader.ReadHeader();

            List<Qso> contacts = new List<Qso>();

            AdifFileReader.Record currentRecord;
            while ((currentRecord = adifReader.ReadRecord()) != null)
            {
                Qso c = GetContact(currentRecord);
                contacts.Add(c);
            }

            return contacts;
        }

        public static Qso GetContact(AdifFileReader.Record record)
        {
            Qso c = new Qso();
            c.Callsign = record["call"];

            // This parsing is horrid. TODO: Figure out how to use IFormatProvider properly.
            string dateStr = record["qso_date"];
            string timeOnStr = record["time_on"];
            DateTime? date = AdifFileReader.ParseAdifDate(dateStr, timeOnStr);
            c.QsoTime = date.Value;

            c.Band = ParseBand(record["band"]);
            c.Operator = record["operator"];
            c.Mode = ParseMode(record["mode"]);
            return c;
        }

        private static Band ParseBand(string band)
        {
            return (Band)Enum.Parse(typeof(Band), "B" + band.ToLowerInvariant());
        }

        private static Mode ParseMode(string mode)
        {
            switch (mode.Trim().ToLowerInvariant())
            {
                case "ssb":
                case "lsb":
                case "usb":
                case "fm":
                    return Mode.Phone;
                case "cw":
                    return Mode.CW;
                case "rtty":
                case "psk31":
                case "psk":
                    return Mode.Data;
                default:
                    return Mode.Unknown;
            }
        }
    }
}