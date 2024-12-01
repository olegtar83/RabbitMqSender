using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMqSender.Database.Abstractions;
using RabbitMqSender.DataClasses;
using RabbitMqSender.DataClasses.Entities;
using RabbitMqSender.DataClasses.Enums;
using RabbitMqSender.Extensions;
using System.Text;
using System.Text.Json;

namespace RabbitMqSender.Consumers
{
    public class PaymentConsumer : IConsumer<PaymentRequest>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<PaymentConsumer> _logger;
        private readonly HttpClient _client;
        public PaymentConsumer(IApplicationDbContext dbContext,
            ILogger<PaymentConsumer> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _client = httpClientFactory.CreateClient("InvoiceClient");

        }
        public async Task Consume(ConsumeContext<PaymentRequest> context)
        {
            var statuses = await _dbContext.PaymentStatus.ToListAsync();
            var statusDictionary = statuses.ToDictionary(x => x.Status, x => x);

            var payment = new Payment()
            {
                JsonMessage = JsonSerializer.Serialize(context.Message),
                ReceivedAt = DateTime.UtcNow,
                PaymentStatus = statusDictionary!.GetValue(Status.Received.GetDescription())!
            };

            _logger.LogInformation($"Received {JsonSerializer.Serialize(context.Message)}");

            var xmlString = ConvertToXml(context.Message);

            _logger.LogInformation($"Converted {xmlString}");

            try
            {
                var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");

                var response = await _client.PostAsync("", httpContent);

                payment.PaymentStatus = response.IsSuccessStatusCode
                 ? statusDictionary!.GetValue(Status.Sent.GetDescription())!
                 : statusDictionary!.GetValue(Status.Error.GetDescription())!;
            }
            catch (HttpRequestException httpEx)
            {
                payment.PaymentStatus = statusDictionary!.GetValue(Status.Error.GetDescription())!;
                _logger.LogError($"Invalid Url for data sending{Environment.NewLine}{httpEx.Message}");
            }
            await _dbContext.Payments.AddAsync(payment);
            await _dbContext.SaveChangesAsync();
        }


        private string ConvertToXml(PaymentRequest paymentRequest)
        {
            var invoicePayment = new InvoicePayment
            {
                Id = paymentRequest.Request.Id.ToString(),
                Debit = paymentRequest.DebitPart.AccountNumber,
                Credit = paymentRequest.CreditPart.AccountNumber,
                Amount = paymentRequest.DebitPart.Amount,
                Currency = paymentRequest.DebitPart.Currency,
                Details = paymentRequest.Details,
                Pack = paymentRequest.Attributes?.Attribute.Find(attr => attr.Code == "pack")?.Attribute ?? string.Empty,
            };

            return invoicePayment.SeriallizeToXml<InvoicePayment>();
        }
    }
}
