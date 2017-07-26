using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Charp_analysis_sample
{
    public class Options
    {
        public string tokenUrl = "https://token.beyondverbal.com/token";
        public string apiKey = "Enter API Key";
        public string startUrl = "https://apiv3.beyondverbal.com/v3/recording/";

    }
    public class AnalysisClient
    {
        public string analysisUrl;
        public string token;
        public string recordingId;
        public void init()
        {
            Options options = new Options();

            var requestData = "apiKey=" + options.apiKey + "&grant_type=client_credentials";
            //auth
            token = authRequest(options.tokenUrl, Encoding.UTF8.GetBytes(requestData));

            //
            var startResponseString = CreateWebRequest(options.startUrl + "start", Encoding.UTF8.GetBytes("{ dataFormat: { type: \"WAV\" } }"), token);
            var startResponseObj = JsonConvert.DeserializeObject<dynamic>(startResponseString);
            if (startResponseObj.status != "success")
            {
                Console.WriteLine("Response Status: " + startResponseObj.status);
                return;
            }
            recordingId = startResponseObj.recordingId.Value;

            ////analysis
            analysisUrl = options.startUrl + recordingId;

            Debug.WriteLine("End Init recID:" + recordingId);
            


        }
        public static async Task RepeatActionEvery(Action<dynamic> action, TimeSpan interval, CancellationTokenSource cancellationToken, Uri url, string token, string recordingId)
        {
            long FromMs = 0;
            while (true)
            {

                Task<long> at = ReadAnalysis(action, cancellationToken, url, token, recordingId, FromMs);

                FromMs = await at;
                Task task = Task.Delay(interval, cancellationToken.Token);


                try
                {
                    await task;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private async static Task<long> ReadAnalysis(Action<dynamic> action, CancellationTokenSource cancelationToken, Uri serviceUri, string accessToken, string recordingId, long fromMs = 0)
        {
            using (var client = new HttpClient() { BaseAddress = serviceUri })
            {
                Console.WriteLine("From ms:" + fromMs);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string path = string.Format("{0}/analysis?fromMs={1}", recordingId, fromMs);

                try
                {
                    var response = await client.GetAsync(path, cancelationToken.Token);
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        dynamic responceContent = JsonConvert.DeserializeObject(content);


                        dynamic result = responceContent.result;
                        try
                        {
                            if (result.analysisSegments != null)
                            {
                                fromMs = result.analysisSegments[result.analysisSegments.Count - 1].offset;
                                //Debug.WriteLine("result:"result);
                                action(result);
                            }

                        }
                        catch (Exception)
                        {

                        }


                        if (result.sessionStatus != null && result.sessionStatus == "Done")
                            cancelationToken.Cancel();



                        return fromMs;
                    }
                    else
                    {
                        return 0;

                    }
                }
                catch (Exception ex)
                {
                    return 0;

                }

            }
        }
        public async Task Analyze(Stream data, Action<dynamic> getanalysis)
        {
            string url = analysisUrl;
            Debug.WriteLine("Start analyze");
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            CancellationTokenSource cts = new CancellationTokenSource();

            using (var client = new HttpClient() { BaseAddress = new Uri(url) })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.Timeout = TimeSpan.FromSeconds(600);

                try
                {


                    //Console.WriteLine("Start Analyse:url" + url + " recID:" + recordingId);
                    Task<HttpResponseMessage> postTask = client.PostAsync(recordingId, new StreamContent(data));


                    Task pooliTask = RepeatActionEvery(getanalysis, TimeSpan.FromSeconds(5), cts, new Uri(url), token, recordingId);//.Wait();

                    var response = await postTask;
                    var content = await response.Content.ReadAsStringAsync();//wait for result

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic responceContent = JsonConvert.DeserializeObject(content, jsonSerializerSettings);
                        dynamic result = responceContent.result;
                        Debug.WriteLine("Analyse result ");
                        //readanalysis(result);
                    }
                    else
                    {

                        //throw new Exception("Analyze exp:" + response.ToString() + "\r\n" + content);
                    }

                    await pooliTask;
                }
                catch (Exception e)
                {
                    //Debug.WriteLine(e.Message);

                    //throw;
                }
            }
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
            request.SendChunked = true;
            request.AllowWriteStreamBuffering = false;
            request.AllowReadStreamBuffering = false;
            request.KeepAlive = true;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }

            using (var response = request.GetResponse() as HttpWebResponse)
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
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
            request.ContentType = "application/octet-stream";
            request.KeepAlive = true;
            request.ServicePoint.SetTcpKeepAlive(false, 0, 0);
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ReadWriteTimeout = 1000000;
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
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
