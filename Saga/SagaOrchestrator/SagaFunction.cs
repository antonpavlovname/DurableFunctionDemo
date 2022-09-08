using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var rollBackCollection = new Stack<Func<Task>>();
            do
            {
                var seatsStatus = await context.CallActivityAsync<SeatsStatus>("BookSeats", seats);
                if (!seatsStatus.Succeed) break;
                rollBackCollection.Push(async () => await context.CallActivityAsync("ReleaseSeats", seats));

                var paymentStatus = await context.CallSubOrchestratorAsync<PaymentStatus>("Pay", "eventinstance", seatsStatus.Price);
                if(!paymentStatus.Succeed) break;
                rollBackCollection.Push(async () => await context.CallActivityAsync("CancelPayment", paymentStatus.TransactionId));

                var registrationStatus = await context.CallActivityAsync<RegistrationStatus>("RegisterAndNotify",
                    new RegistrationData(seats, paymentStatus.TransactionId, customerData));
                
                if (!registrationStatus.Succeed) break;
                
                return new BookingStatus(true, registrationStatus.OrderId);
            } while (false);

            while (rollBackCollection.TryPop(out var rollBack))
            {
                await rollBack();
            }

            return new BookingStatus(false, null);
        }

        [FunctionName("BookSeats")]
        public static SeatsStatus BookSeats([ActivityTrigger] int[] seats, ILogger log)
        {
            log.LogWarning($"Booking seats '{string.Join(", ", seats.Select(seat => seat.ToString()))}'.");
            return new SeatsStatus(true, 1000);
        }

        [FunctionName("ReleaseSeats")]
        public static void ReleaseSeats([ActivityTrigger] int[] seats, ILogger log)
        {
            log.LogWarning($"Releasing seats '{string.Join(", ", seats.Select(seat => seat.ToString()))}'.");
        }


        [FunctionName("Pay")]
        public static async Task<PaymentStatus> Pay([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            const string eventName = "payEvent";
            var price = context.GetInput<decimal>();
            var result = false;
            log.LogWarning($"Paying seats {price}$. Raise event '{eventName}'");

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

            return new PaymentStatus(result, result ? Guid.NewGuid() : Guid.Empty);
        }

        [FunctionName("CancelPayment")]
        public static void CancelPayment([ActivityTrigger] Guid paymentId, ILogger log)
        {
            log.LogWarning($"Cancel payment '{paymentId}'.");
        }


        [FunctionName("RegisterAndNotify")]
        public static RegistrationStatus RegisterAndNotify([ActivityTrigger] RegistrationData registrationData, ILogger log)
        {
            log.LogWarning($"Register And Notify {registrationData}.");
            return new RegistrationStatus(true, "2022 0009 1234");
        }
    }
}