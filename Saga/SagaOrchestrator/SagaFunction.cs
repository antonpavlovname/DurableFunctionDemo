using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SagaOrchestrator
{
    public static class SagaFunction
    {
        public record SeatsStatus(bool Succeed, decimal Price);

        public record PaymentStatus(bool Succeed, Guid TransactionId);

        public record RegistrationStatus(bool Succeed, string OrderId);

        public record BookingStatus(bool Succeed, string OrderId);

        public record RegistrationData(int[] Seats, Guid TransactionId, string CustomerData);

        [FunctionName("Saga")]
        public static async Task<BookingStatus> SagaOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var seats = new[] { 5, 7 };
            var customerData = "test customer";
            var seatsStatus = await context.CallActivityAsync<SeatsStatus>("BookSeats", seats);
            var paymentStatus = await context.CallSubOrchestratorAsync<PaymentStatus>("Pay", seatsStatus.Price);
            var registrationStatus = await context.CallActivityAsync<RegistrationStatus>("RegisterAndNotify", new RegistrationData(seats, paymentStatus.TransactionId, customerData));

            return new BookingStatus(true, registrationStatus.OrderId);
        }

        [FunctionName("BookSeats")]
        public static SeatsStatus BookSeats([ActivityTrigger] int[] seats, ILogger log)
        {
            log.LogInformation($"Booking seats {string.Join(", ", seats.ToString())}.");
            return new SeatsStatus(true, 1000);
        }

        [FunctionName("Pay")]
        public static async Task<PaymentStatus> Pay([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            const string eventName = "payEvent";
            var price = context.GetInput<decimal>();
            var result = false;
            log.LogInformation($"Paying seats {price}$. Raise event '{eventName}'");

            using var timeoutCts = new CancellationTokenSource();

            var expiration = context.CurrentUtcDateTime.AddMinutes(3);
            var timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

            var myEventResult =
                context.WaitForExternalEvent<bool>(eventName);

            var winner = await Task.WhenAny(myEventResult, timeoutTask);

            if (winner == myEventResult)
            {
                result = myEventResult.Result;
            }
            else
            {
                log.LogError("Timeout");
            }

            if (!timeoutTask.IsCompleted)
            {
                timeoutCts.Cancel(); // All pending timers must be complete or canceled before the function exits.
            }

            return new PaymentStatus(result, result ? new Guid(): Guid.Empty);
        }

        [FunctionName("RegisterAndNotify")]
        public static RegistrationStatus RegisterAndNotify([ActivityTrigger] RegistrationData registrationData, ILogger log)
        {
            log.LogInformation($"Booking seats {registrationData}.");
            return new RegistrationStatus(true, "2022 0009 1234");
        }
    }
}