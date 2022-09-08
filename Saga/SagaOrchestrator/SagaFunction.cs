using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

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
            var paymentStatus = await context.CallActivityAsync<PaymentStatus>("Pay", seatsStatus.Price);
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
        public static PaymentStatus Pay([ActivityTrigger] decimal price, ILogger log)
        {
            log.LogInformation($"Paying seats {price}$.");
            return new PaymentStatus(true, new Guid());
        }

        [FunctionName("RegisterAndNotify")]
        public static RegistrationStatus RegisterAndNotify([ActivityTrigger] RegistrationData registrationData, ILogger log)
        {
            log.LogInformation($"Booking seats {registrationData}.");
            return new RegistrationStatus(true, "2022 0009 1234");
        }
    }
}