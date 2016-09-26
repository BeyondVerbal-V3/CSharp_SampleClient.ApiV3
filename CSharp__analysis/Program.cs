using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Quobject.SocketIoClientDotNet.Client;

namespace CSharp__analysis
{
    public class Options
    {
        public string tokenUrl = "https://token.beyondverbal.com/token";
        public string apiKey = "Enter API Key";
        public string startUrl = "https://testapiv3.beyondverbal.com/v3/recording/";
        public string postFilePath = @"C:\YouFile.wav";
    }
    class Program
    {
        static void Main(string[] args)
        {
            //Analysis sample via websocket
            Options options = new Options();
            var socket = IO.Socket("http://analysis.beyondverbal.com");
            
           
            var requestData = "apiKey=" + options.apiKey + "&grant_type=client_credentials";
            //auth
            var token = authRequest(options.tokenUrl, Encoding.UTF8.GetBytes(requestData));

            //start
            var startResponseString = CreateWebRequest(options.startUrl + "start", Encoding.UTF8.GetBytes("{ dataFormat: { type: \"WAV\" } }"), token);

            var startResponseObj = JsonConvert.DeserializeObject<dynamic>(startResponseString);
            if (startResponseObj.status != "success")
            {
                Console.WriteLine("Response Status: " + startResponseObj.status);
                return;
            }
            var recordingId = startResponseObj.recordingId.Value;
           
            socket.Emit("join", recordingId);
            Console.WriteLine("Connected to socket:"+recordingId);
           
            socket.On("update", data => {

                // get the json data from the server message
                //var jobject = data as JToken;
                Console.WriteLine("\r\n");
                Console.WriteLine(data);
                
            });
           
            //analysis
            string analysisUrl = options.startUrl + recordingId;
            var bytes = System.IO.File.ReadAllBytes(options.postFilePath);
            var analysisResponseString = CreateWebRequest(analysisUrl, bytes, token);
            
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("\r\nAnalysis on finish request:");
            Console.WriteLine(analysisResponseString);
            // disconnect from the server
            socket.Close();
            
            Console.WriteLine("-------------------");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            
            
           
        }
        
        private static string authRequest(string url, byte[] data)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ServicePoint.SetTcpKeepAlive(false, 0, 0);
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ReadWriteTimeout = 1000000;
            request.Timeout = 10000000;
            request.SendChunked = false;
            request.AllowWriteStreamBuffering = true;
            request.AllowReadStreamBuffering = false;
            request.KeepAlive = true;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new System.IO.StreamReader(responseStream, Encoding.UTF8))
            {
                var res = streamReader.ReadToEnd();
                dynamic responceContent = JsonConvert.DeserializeObject(res, jsonSerializerSettings);
                return responceContent.access_token;

            }
        }

        private static string CreateWebRequest(string url, byte[] data, string token = null)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.KeepAlive = true;
            request.ServicePoint.SetTcpKeepAlive(true, 10000, 10000);

            request.Timeout = 10000000;
            request.SendChunked = false;
            request.AllowWriteStreamBuffering = true;
            request.AllowReadStreamBuffering = false;
            if (string.IsNullOrEmpty(token) == false)
                request.Headers.Add("Authorization", "Bearer " + token);

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new System.IO.StreamReader(responseStream, Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
