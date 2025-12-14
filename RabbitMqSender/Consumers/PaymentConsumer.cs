using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using RabbitMqSender.Database.Abstractions;
using RabbitMqSender.DataClasses;
using RabbitMqSender.DataClasses.Entities;
using RabbitMqSender.DataClasses.Enums;
using RabbitMqSender.Extensions;
using System.Text;
using System.Text.Json;

namespace RabbitMqSender.Consumers
{
    public class PaymentConsumer(
        IApplicationDbContext dbContext,
        ILogger<PaymentConsumer> logger,
        IHttpClientFactory httpClientFactory) : IConsumer<PaymentRequest>
    {
        private readonly IApplicationDbContext _dbContext = dbContext;
        private readonly ILogger<PaymentConsumer> _logger = logger;
        private readonly HttpClient _client = httpClientFactory.CreateClient("InvoiceClient");
        private readonly ObjectPool<StringBuilder> _pool = new DefaultObjectPool<StringBuilder>(
                new StringBuilderPooledObjectPolicy(),
                20);

        public async Task Consume(ConsumeContext<PaymentRequest> context)
        {
            var statuses = await _dbContext.PaymentStatus.ToListAsync();
            var statusDictionary = statuses.ToDictionary(x => x.Status, x => x);

            var payment = new Payment()
            {
                JsonMessage = JsonSerializer.Serialize(context.Message),
                ReceivedAt = DateTime.UtcNow,
                PaymentStatus = statusDictionary![Status.Received.GetDescription()]!
            };

            await _dbContext.Payments.AddAsync(payment);
            await _dbContext.SaveChangesAsync();

            var xmlString = BuildXml(context.Message);

            try
            {
                using var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");
                var response = await _client.PostAsync("", httpContent);

                payment.PaymentStatus = response.IsSuccessStatusCode
                    ? statusDictionary![Status.Sent.GetDescription()]!
                    : statusDictionary![Status.Error.GetDescription()]!;
            }
            catch (HttpRequestException httpEx)
            {
                payment.PaymentStatus = statusDictionary![Status.Error.GetDescription()]!;
                _logger.LogError(httpEx, "Failed to send payment");
            }

            await _dbContext.SaveChangesAsync();
        }

        private string BuildXml(PaymentRequest paymentRequest)
        {
            var sb = _pool.Get();
            try
            {
                sb.Clear();

                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.Append("<InvoicePayment>");
                sb.Append("<Id>").Append(paymentRequest.Request.Id).Append("</Id>");
                sb.Append("<Debit>").Append(paymentRequest.DebitPart.AccountNumber).Append("</Debit>");
                sb.Append("<Credit>").Append(paymentRequest.CreditPart.AccountNumber).Append("</Credit>");
                sb.Append("<Amount>").Append(paymentRequest.DebitPart.Amount).Append("</Amount>");
                sb.Append("<Currency>").Append(paymentRequest.DebitPart.Currency).Append("</Currency>");
                sb.Append("<Details>").Append(paymentRequest.Details).Append("</Details>");

                var pack = paymentRequest.Attributes?.Attribute.Find(attr => attr.Code == "pack")?.Attribute ?? "";
                sb.Append("<Pack>").Append(pack).Append("</Pack>");

                sb.Append("</InvoicePayment>");

                return sb.ToString();
            }
            finally
            {
                _pool.Return(sb);
            }
        }
    }

    public class StringBuilderPooledObjectPolicy : IPooledObjectPolicy<StringBuilder>
    {
        public StringBuilder Create() => new(256);
        public bool Return(StringBuilder obj)
        {
            obj.Clear();
            return true;
        }
    }
}