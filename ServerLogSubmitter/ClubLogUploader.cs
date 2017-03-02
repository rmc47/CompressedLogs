using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerLogSubmitter
{
    public class ClubLogUploader
    {
        public void UploadToClubLog(string adifPath, string callsign, string username, string password, string apiKey)
        {
            NameValueCollection uploadParameters = new NameValueCollection();
            uploadParameters["email"] = username;
            uploadParameters["password"] = password;
            uploadParameters["callsign"] = callsign;
            uploadParameters["api"] = apiKey;

            NameValueCollection files = new NameValueCollection ();
            files["file"] = adifPath;
            string response = sendHttpRequest("http://www.clublog.org/putlogs.php", uploadParameters, files);
        }

        private static string sendHttpRequest(string url, NameValueCollection values, NameValueCollection files = null)
        {
            System.Net.ServicePointManager.Expect100Continue = false;

            string boundary = "--------" + DateTime.Now.Ticks.ToString("x");
            // The first boundary
            byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "\r\n");
            // The last boundary
            byte[] trailer = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");
            // Create the request and set parameters
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.KeepAlive = false;
            request.Credentials = System.Net.CredentialCache.DefaultCredentials;
            // Get request stream
            Stream requestStream = request.GetRequestStream();
            if (files != null)
            {
                foreach (string key in files.Keys)
                {
                    if (File.Exists(files[key]))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[2048];
                        byte[] formItemBytes =
                        System.Text.Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n", key, Path.GetFileName(files[key])));
                        requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                        requestStream.Write(formItemBytes, 0, formItemBytes.Length);
                        using (FileStream fileStream = new FileStream(files[key], FileMode.Open, FileAccess.Read))
                        {
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                // Write file content to stream, byte by byte
                                requestStream.Write(buffer, 0, bytesRead);
                            }
                            fileStream.Close();
                        }
                    }
                }
            }
            foreach (string key in values.Keys)
            {
                // Write item to stream
                byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n", key, values[key]));
                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);
            }

            // Write trailer and close stream
            requestStream.Write(trailer, 0, trailer.Length);
            requestStream.Close();
            try
            {
                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    string errorText = reader.ReadToEnd();
                }
                throw;
            }
        }
    }
}
