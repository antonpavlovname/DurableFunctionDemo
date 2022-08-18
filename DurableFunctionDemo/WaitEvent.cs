using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableFunctionDemo
{
    public static class WaitEvent
    {
        private const string EventName = "MyEvent";
        [FunctionName("WaitEvent")]
        public static async Task<bool> WaitEventOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation("In {functionName}", nameof(GetRandom));

            var random = await context.CallActivityAsync<int>(nameof(GetRandom), null);

            log.LogWarning("Random number {random}, event name {EventName}", random, EventName);
            var result = false;
            using var timeoutCts = new CancellationTokenSource();

            var expiration = context.CurrentUtcDateTime.AddMinutes(1);
            var timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);
                
            var myEventResult =
                context.WaitForExternalEvent<int>(EventName);

            var winner = await Task.WhenAny(myEventResult, timeoutTask);
            
            if (winner == myEventResult)
            {
                result = myEventResult.Result == random;
            }
            else
            {
                log.LogError("Timeout");
            }

            if (!timeoutTask.IsCompleted)
            {
                timeoutCts.Cancel(); // All pending timers must be complete or canceled before the function exits.
            }

            return result;
        }

        [FunctionName(nameof(GetRandom))]
        public static int GetRandom([ActivityTrigger] string input)
        {
            return Random.Shared.Next(99);
        }
    }
}