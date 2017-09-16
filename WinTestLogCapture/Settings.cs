using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTestLogCapture
{
    internal sealed class Settings
    {
        public static string DatabasePath
        {
            get { return ConfigurationManager.AppSettings["DatabasePath"]; }
        }

        public static string LogUploadUrl
        {
            get { return ConfigurationManager.AppSettings["LogUploadUrl"]; }
        }

        public static int UploadInterval
        {
            get { return int.Parse(ConfigurationManager.AppSettings["UploadInterval"]); }
        }
    }
}
