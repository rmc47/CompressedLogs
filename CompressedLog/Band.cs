using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLog
{
    public enum Band
    {
        // No more than 16 values!
        Unknown = 0,
        B160m, // 1
        B80m, // 2
        B60m, // 3
        B40m, // 4
        B30m, // 5
        B20m, // 6
        B17m, // 7
        B15m, // 8
        B12m, // 9
        B10m, // 10
        B6m, // 11
        SatSO50, // 12
        SatFO29, // 13
    }
}
