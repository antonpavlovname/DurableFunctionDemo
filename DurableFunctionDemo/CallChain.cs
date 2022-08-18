using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionDemo
{
    public static class CallChain
    {
        [FunctionName("CallChain")]
        public static async Task<string> CallChainOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogWarning("In {functionName}", nameof(CallChainOrchestrator));
            log.LogWarning("Before {functionName}", nameof(Link1));
            var link1 = await context.CallActivityAsync<int>(nameof(Link1), context.GetInput<string>());
            log.LogWarning("Before {functionName}", nameof(Link2));
            var link2 = await context.CallActivityAsync<string>(nameof(Link2), link1);
            log.LogWarning("Before {functionName}", nameof(Link3));
            var result = await context.CallActivityAsync<string>(nameof(Link3), link2);

            return result;
        }

        [FunctionName(nameof(Link1))]
        public static async Task<int> Link1([ActivityTrigger] string input, ILogger log)
        {
            log.LogInformation("In {functionName}; {data}", nameof(Link1), input);
            await Task.Delay(TimeSpan.FromSeconds(10));
            return int.Parse(input);
        }

        [FunctionName(nameof(Link2))]
        public static async Task<string> Link2([ActivityTrigger] int toFormat, ILogger log)
        {
            log.LogInformation("In {functionName}, {data}", nameof(Link2), toFormat);
            await Task.Delay(TimeSpan.FromSeconds(10));
            return $"Formatted data {toFormat}";
        }

        [FunctionName(nameof(Link3))]
        public static async Task<string> Link3([ActivityTrigger] string toWrap, ILogger log)
        {
            log.LogInformation("In {functionName}, {data}", nameof(Link3), toWrap);
            await Task.Delay(TimeSpan.FromSeconds(10));
            return $"Wrapped data [{toWrap}]";
        }
    }
}