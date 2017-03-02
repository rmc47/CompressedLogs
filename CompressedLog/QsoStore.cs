using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedLog
{
    public sealed class QsoStore
    {
        public void AddQso(Qso q)
        {
            using (SQLiteConnection conn = GetConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO qsos (band, callsign, mode, operator, qsotime, processed) VALUES (@band, @callsign, @mode, @operator, @qsotime, @processed);";
                    AddStandardParameters(q, cmd);
                    cmd.Parameters.AddWithValue("@processed", false);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MarkQsoProcessed(Qso q)
        {
            using (SQLiteConnection conn = GetConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE qsos SET processed=1 WHERE band=@band AND callsign=@callsign AND mode=@mode AND operator=@operator AND qsotime=@qsotime;";
                    AddStandardParameters(q, cmd);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Qso> GetUnprocessedQsos()
        {
            using (SQLiteConnection conn = GetConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM qsos WHERE processed=0;";
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        List<Qso> qsos = new List<Qso>();
                        while (reader.Read())
                        {
                            qsos.Add(LoadFromDataReader(reader));
                        }
                        return qsos;
                    }
                }
            }
        }

        public bool QsoExists(Qso q)
        {
            using (SQLiteConnection conn = GetConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM qsos WHERE band=@band AND callsign=@callsign AND mode=@mode AND operator=@operator AND qsotime>@timelower AND qsotime<@timeupper;";
                    AddStandardParameters(q, cmd);
                    cmd.Parameters.AddWithValue("@timelower", q.QsoTime.AddSeconds(-60));
                    cmd.Parameters.AddWithValue("@timeupper", q.QsoTime.AddSeconds(60));
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public void DeleteQso(Qso q)
        {
            using (SQLiteConnection conn = GetConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM qsos WHERE band=@band AND callsign=@callsign AND mode=@mode AND operator=@operator AND qsotime=@qsotime;";
                    AddStandardParameters(q, cmd);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private Qso LoadFromDataReader(SQLiteDataReader dr)
        {
            Qso q = new Qso();
            q.Band = (Band)Enum.Parse(typeof(Band), dr.GetString(dr.GetOrdinal("band")));
            q.Callsign = dr.GetString(dr.GetOrdinal("callsign"));
            q.Mode = (Mode)Enum.Parse(typeof(Mode), dr.GetString(dr.GetOrdinal("mode")));
            q.Operator = dr.GetString(dr.GetOrdinal("operator"));
            q.QsoTime = dr.GetDateTime(dr.GetOrdinal("qsotime"));
            return q;
        }

        private SQLiteConnection GetConnection()
        {
            SQLiteConnection conn = new SQLiteConnection();
            SQLiteConnectionStringBuilder csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = Path.Combine("C:\\CompressedLogs", "qsodb.sqlite3");
            if (!Directory.Exists(Path.GetDirectoryName(csb.DataSource)))
                Directory.CreateDirectory(Path.GetDirectoryName(csb.DataSource));
            conn.ConnectionString = csb.ConnectionString;
            conn.Open();
            return conn;
        }

        private void AddStandardParameters(Qso q, SQLiteCommand cmd)
        {
            cmd.Parameters.AddWithValue("@band", q.Band.ToString());
            cmd.Parameters.AddWithValue("@callsign", q.Callsign);
            cmd.Parameters.AddWithValue("@mode", q.Mode.ToString());
            cmd.Parameters.AddWithValue("@operator", q.Operator);
            cmd.Parameters.AddWithValue("@qsotime", q.QsoTime);
        }
    }
}
