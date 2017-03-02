using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLog
{
    public class QsoCompressor
    {
        public static readonly DateTime s_DateTimeEpoch = new DateTime(2017, 03, 01);
        private const int c_HeaderLength = 5;

        public byte[] CompressQso(Qso q)
        {
            if (q == null)
                throw new ArgumentNullException("q", "QSO to compress is null");

            // 0: start byte
            // 1-2: date time offset
            // 3: band and operator
            // 4: mode and callsign length
            // 5-(n-1): callsign (ASCII)
            int callsignLength = Encoding.ASCII.GetByteCount(q.Callsign);
            int compressedLength = callsignLength + c_HeaderLength;
            byte[] compressedBytes = new byte[compressedLength];

            // Byte 0: start byte, always 0xFF
            compressedBytes[0] = 0xFF;

            // Bytes 1-2: number of minutes since epoch
            TimeSpan timeOffset = q.QsoTime.Subtract(s_DateTimeEpoch);
            byte[] dateTimeBytes = BitConverter.GetBytes((uint)timeOffset.TotalMinutes);
            Buffer.BlockCopy(dateTimeBytes, 0, compressedBytes, 1, 2);

            // Byte 3: band and operator, 4 bits each
            compressedBytes[3] = (byte)((GetBandByte(q.Band) & 0x0F) | (GetOperatorByte(q.Operator) << 4 & 0xF0));

            // Byte 4: mode (bits 6-7) and callsign length (bits 0-5)
            compressedBytes[4] = (byte)((GetModeByte(q.Mode) << 6 & 0xC0) | callsignLength & 0x3F);

            // Bytes 5 onwards: callsign
            byte[] callsignBytes = Encoding.ASCII.GetBytes(q.Callsign);
            Buffer.BlockCopy(callsignBytes, 0, compressedBytes, 5, callsignBytes.Length);

            return compressedBytes;
        }

        public Qso UncompressQso(byte[] buff, int start)
        {
            return UncompressQso(buff, ref start);
        }

        public Qso UncompressQso(byte[] buff, ref int start)
        {
            if (buff == null)
                throw new ArgumentNullException("buff", "Compressed data buffer null");
            if (start >= buff.Length)
                throw new ArgumentOutOfRangeException("start", "Start position beyond end of compressed data buffer");
            if (start >= (buff.Length - c_HeaderLength))
                throw new ArgumentOutOfRangeException("start", "Start position does not leave enough data for QSO header in buffer");

            // Byte 0: start byte, always 0xFF
            if (buff[start] != 0xFF)
                throw new ArgumentException("buff", "Compressed data does not begin with expected start byte");

            Qso q = new Qso();

            // Byte 1-2: minutes since epoch
            UInt16 minutesSinceEpoch = BitConverter.ToUInt16(buff, start + 1);
            q.QsoTime = s_DateTimeEpoch.AddMinutes(minutesSinceEpoch);

            // Byte 3: band and operator, 4 bits each
            byte band = (byte)(buff[start + 3] & 0x0F);
            q.Band = GetBand(band);
            byte op = (byte)((buff[start + 3] & 0xF0) >> 4);
            q.Operator = GetOperator(op);

            // Byte 4: mode (bits 6-7) and callsign length (bits 0-5)
            byte modeByte = (byte)((buff[start + 4] & 0xC0) >> 6);
            q.Mode = GetMode(modeByte);
            int callsignLength = buff[start + 4] & 0x3F;

            if (buff.Length < (start + c_HeaderLength + callsignLength))
                throw new ArgumentException("buff", "Callsign length runs beyond end of compressed data");
            
            // Byte 5+: callsign, ASCII
            q.Callsign = Encoding.ASCII.GetString(buff, start + 5, callsignLength);

            start += c_HeaderLength + callsignLength;

            return q;
        }

        private byte GetBandByte(Band b)
        {
            switch (b)
            {
                case Band.B160m: return 1;
                case Band.B80m: return 2;
                case Band.B60m: return 3;
                case Band.B40m: return 4;
                case Band.B30m: return 5;
                case Band.B20m: return 6;
                case Band.B17m: return 7;
                case Band.B15m: return 8;
                case Band.B12m: return 9;
                case Band.B10m: return 10;
                case Band.B6m: return 11;
                case Band.SatSO50: return 12;
                case Band.SatFO29: return 13;
                default: return 0;
            }
        }

        private Band GetBand(byte b)
        {
            switch (b)
            {
                case 1: return Band.B160m;
                case 2: return Band.B80m;
                case 3: return Band.B60m;
                case 4: return Band.B40m;
                case 5: return Band.B30m;
                case 6: return Band.B20m;
                case 7: return Band.B17m;
                case 8: return Band.B15m;
                case 9: return Band.B12m;
                case 10: return Band.B10m;
                case 11: return Band.B6m;
                case 12: return Band.SatSO50;
                case 13: return Band.SatFO29;
                default: return Band.Unknown;
            }
        }

        private byte GetModeByte(Mode m)
        {
            switch (m)
            {
                case Mode.CW: return 1;
                case Mode.Phone: return 2;
                case Mode.Data: return 3;
                default: return 0;
            }
        }

        private Mode GetMode(byte b)
        {
            switch (b)
            {
                case 1: return Mode.CW;
                case 2: return Mode.Phone;
                case 3: return Mode.Data;
                default: return Mode.Unknown;
            }
        }

        private byte GetOperatorByte(string op)
        {
            if (string.IsNullOrWhiteSpace(op))
                return 0;
            switch (op.Trim().ToUpperInvariant())
            {
                case "M0IDA": return 1;
                case "M0VFC": return 2;
                case "M1ACB": return 3;
                default: return 0;
            }
        }

        private string GetOperator(byte opByte)
        {
            switch (opByte)
            {
                case 1: return "M0IDA";
                case 2: return "M0VFC";
                case 3: return "M1ACB";
                default: return "UNKNOWN";
            }
        }
    }
}
