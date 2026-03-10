using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using RabbitMqSender.Database.Abstractions;
using RabbitMqSender.DataClasses;
using RabbitMqSender.DataClasses.Entities;
using RabbitMqSender.DataClasses.Enums;
using RabbitMqSender.Extensions;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace RabbitMqSender.Consumers
{
    public sealed class PaymentConsumer(
        IApplicationDbContext dbContext,
        ILogger<PaymentConsumer> logger,
        IHttpClientFactory httpClientFactory) : IConsumer<PaymentRequest>
    {
        private readonly IApplicationDbContext _dbContext = dbContext;
        private readonly ILogger<PaymentConsumer> _logger = logger;
        private readonly HttpClient _client = httpClientFactory.CreateClient("InvoiceClient");
        private readonly ObjectPool<StringBuilder> _pool = new DefaultObjectPool<StringBuilder>(
                new StringBuilderPooledObjectPolicy(),
                Environment.ProcessorCount * 2);
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly Dictionary<string, PaymentStatus> _statuses = dbContext.
                                PaymentStatus.AsNoTracking().ToDictionary(x => x.Status, x => x);

        public async Task Consume(ConsumeContext<PaymentRequest> context)
        {
            var receivedStatus = _statuses[Status.Received.GetDescription()];
            var sentStatus = _statuses[Status.Sent.GetDescription()];
            var errorStatus = _statuses[Status.Error.GetDescription()];

            var bufferWriter = new ArrayBufferWriter<byte>(1024);
            using (var utf8JsonWriter = new Utf8JsonWriter(bufferWriter))
            {
                JsonSerializer.Serialize(utf8JsonWriter, context.Message, _jsonOptions);
            }
            var jsonMessage = Encoding.UTF8.GetString(bufferWriter.WrittenSpan);

            var payment = new Payment()
            {
                JsonMessage = jsonMessage,
                ReceivedAt = DateTime.UtcNow,
                PaymentStatus = receivedStatus
            };

            await _dbContext.Payments.AddAsync(payment, context.CancellationToken);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            var xmlString = BuildXml(context.Message);

            try
            {
                using var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");
                var response = await _client.PostAsync("", httpContent);

                payment.PaymentStatus = response.IsSuccessStatusCode
                    ? sentStatus
                    : errorStatus;
            }
            catch (HttpRequestException httpEx)
            {
                payment.PaymentStatus = errorStatus;
                _logger.LogError(httpEx, "Failed to send payment");
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }

        private string BuildXml(PaymentRequest paymentRequest)
        {
            var sb = _pool.Get();
            try
            {
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.Append("<InvoicePayment>");
                sb.AppendFormattedValue(paymentRequest.Request.Id, "Id");
                sb.Append("<Debit>").Append(paymentRequest.DebitPart.AccountNumber).Append("</Debit>");
                sb.Append("<Credit>").Append(paymentRequest.CreditPart.AccountNumber).Append("</Credit>");
                sb.AppendFormattedValue(paymentRequest.DebitPart.Amount, "Amount");
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
}
