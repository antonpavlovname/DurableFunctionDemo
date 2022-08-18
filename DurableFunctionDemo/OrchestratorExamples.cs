using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionDemo
{
    public static class OrchestratorExamples
    {
        [FunctionName(nameof(HttpStart))]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var sampleName = req.RequestUri.ParseQueryString()["name"];
            var sampleData = req.RequestUri.ParseQueryString()["data"];
            log.LogInformation($"Starting orchestration '{sampleName}'.");
            
            string instanceId = await starter.StartNewAsync(sampleName, null, sampleData);

            log.LogInformation($"Started orchestration '{sampleName}' with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}