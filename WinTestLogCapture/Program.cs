using CompressedLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading;

namespace WinTestLogCapture
{
    class Program
    {
        public static void Main()
        {
            //new System.Threading.Timer(_ => LogUploader.UploadOutstandingQsos(), null, 0, 5 * 60 * 1000);

            // Start watching our network interfaces once every minute to check they're still all there
            NetworkInterfaceManager interfaceManager = new NetworkInterfaceManager();
            new Timer(_ => {
                try
                {
                    interfaceManager.CheckSocketListeners();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }, null, 0, 60 * 1000);

            LogUploader logUploader = new LogUploader();
            new Timer(_ => {
                try
                {
                    logUploader.UploadOutstandingQsos();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }, null, 0, Settings.UploadInterval * 1000);

            Console.ReadLine();
            Console.WriteLine("Closing sockets...");
            interfaceManager.CloseSocketListeners();
            //TODO: Rebind automatically if socket closes
        }
    }
}
