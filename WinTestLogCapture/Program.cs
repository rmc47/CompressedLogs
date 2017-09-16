using CompressedLog;
using System.Configuration;
using System.Net;

namespace WinTestLogCapture
{
    class Program
    {
        private static IPEndPoint s_BroadcastEndpoint;
        private static IPEndPoint s_SourceEndpoint;
        private static WtSocketListener s_SocketListener;

        public static void Main()
        {
            QsoStore store = new QsoStore(ConfigurationSettings.AppSettings["DatabasePath"]);
            new System.Threading.Timer(_ => LogUploader.UploadOutstandingQsos(), null, 0, 5 * 60 * 1000);


            s_BroadcastEndpoint = new IPEndPoint(IPAddress.Parse(ConfigurationSettings.AppSettings["BroadcastAddress"]), 9871);
            s_SourceEndpoint = new IPEndPoint(IPAddress.Parse(ConfigurationSettings.AppSettings["SourceAddress"]), 9871);

            s_SocketListener = new WtSocketListener(s_SourceEndpoint, s_BroadcastEndpoint);
            s_SocketListener.StartListening();

            //TODO: Rebind automatically if socket closes
        }
    }
}
