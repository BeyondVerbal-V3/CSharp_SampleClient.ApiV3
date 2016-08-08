using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charp_analysis_sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
            Console.WriteLine("finish");
            Console.ReadKey();
        }
        public static async Task Run()
        {
            AnalysisClient client = new AnalysisClient();
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            client.init();

            using (Stream stream = File.Open(@"Wav File Path", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                await client.Analyze(stream, (result) =>
                {
                    foreach (dynamic segment in result.analysisSegments)
                    {

                        Console.WriteLine(JsonConvert.SerializeObject(segment, jsonSerializerSettings));
                    }

                });

            }

            //return Task.FromResult(true);
        }
    }
}
