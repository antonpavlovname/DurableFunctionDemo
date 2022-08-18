using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionDemo
{
    public static class CallTwoChains
    {
        [FunctionName("CallTwoChains")]
        public static async Task<string> CallTwoChainsOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogWarning("In {functionName}", nameof(CallTwoChainsOrchestrator));

            var task1 = context.CallSubOrchestratorAsync<string>("CallChain", 111);
            var task2 = context.CallSubOrchestratorAsync<string>("CallChain", 222);
            await Task.WhenAll(task1, task2);
            return $"{task1.Result}; {task2.Result}";
        }
    }
}