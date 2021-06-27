using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace geekburger.labelloader
{
    class Program
    {
        static string subscriptionKey = "10e1545020f94259afb7f48e276ee1a7";
        static string endpoint = "https://ocrgeekburger.cognitiveservices.azure.com/";

        private const string READ_TEXT_URL_IMAGE = "https://intelligentkioskstore.blob.core.windows.net/visionapi/suggestedphotos/3.png";
        
        static void Main(string[] args)
        {
            ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
            ReadFileUrl(client, READ_TEXT_URL_IMAGE).Wait();
           
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=labelloaderproductimages;AccountKey=nrjPqOqdt3DRA5vj69qkcEOkeIYI64hnkuYfRvfo9LsBFZTanLoRt9gCYDQx7K2k9fxPao4+OagInG67Pdbt+Q==;EndpointSuffix=core.windows.net");

            Console.ReadKey();
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task ReadFileUrl(ComputerVisionClient client, string urlFile)
        {
            var textHeaders = await client.ReadAsync(urlFile);
            string operationLocation = textHeaders.OperationLocation;

            Thread.Sleep(2000);
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            ReadOperationResult results;
           
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            
            var words = from r in textUrlFileResults
                        from l in r.Lines
                        from w in l.Words
                        select w.Text;

            var name = "teste";
            var text = string.Join(" ", words.ToArray());

            addQueue(name, text);
        }

        private static void addQueue(string name, string text)
        {
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=;AccountKey=;EndpointSuffix=core.windows.net");
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("product-images");

            queue.CreateIfNotExistsAsync();
            queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new { name, text })));
        }
    }
}
