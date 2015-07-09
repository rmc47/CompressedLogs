using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLog
{
    public class Qso
    {
        public DateTime QsoTime { get; set; }
        public string Operator { get; set; }
        public string Callsign { get; set; }
        public Mode Mode { get; set; }
        public Band Band { get; set; }
    }
}
