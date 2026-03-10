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
                Environment.ProcessorCount * 2);
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly Dictionary<string, PaymentStatus> _statuses = dbContext.
                                PaymentStatus.AsNoTracking().ToDictionary(x => x.Status, x => x);

        public async Task Consume(ConsumeContext<PaymentRequest> context)
        {
            byte[]? jsonBytes = null;
            byte[]? rentedBuffer = null;
            try
            {
                rentedBuffer = ArrayPool<byte>.Shared.Rent(1024);
                using var utf8JsonWriter = new Utf8JsonWriter(new MemoryStream(rentedBuffer));
                JsonSerializer.Serialize(utf8JsonWriter, context.Message, _jsonOptions);
                jsonBytes = rentedBuffer.AsSpan(0, (int)utf8JsonWriter.BytesCommitted).ToArray();
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            var payment = new Payment()
            {
                JsonMessage = Encoding.UTF8.GetString(jsonBytes),
                ReceivedAt = DateTime.UtcNow,
                PaymentStatus = _statuses![Status.Received.GetDescription()]!
            };

            await _dbContext.Payments.AddAsync(payment, context.CancellationToken);

            var xmlString = BuildXml(context.Message);

            try
            {
                using var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");
                var response = await _client.PostAsync("", httpContent);

                payment.PaymentStatus = response.IsSuccessStatusCode
                    ? _statuses![Status.Sent.GetDescription()]!
                    : _statuses![Status.Error.GetDescription()]!;
            }
            catch (HttpRequestException httpEx)
            {
                payment.PaymentStatus = _statuses![Status.Error.GetDescription()]!;
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
                AppendFormattedValue(sb, paymentRequest.Request.Id, "Id");
                sb.Append("<Debit>").Append(paymentRequest.DebitPart.AccountNumber).Append("</Debit>");
                sb.Append("<Credit>").Append(paymentRequest.CreditPart.AccountNumber).Append("</Credit>");
                AppendFormattedValue(sb, paymentRequest.DebitPart.Amount, "Amount");
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

    private static void AppendFormattedValue<T>(StringBuilder sb, T value, string elementName)
        where T : ISpanFormattable
        {
            sb.Append('<').Append(elementName).Append('>');

            Span<char> buffer = stackalloc char[64];
            if (value.TryFormat(buffer, out int charsWritten, default, null))
            {
                sb.Append(buffer[..charsWritten]);
            }
            else
            {
                sb.Append(value.ToString());
            }

            sb.Append('<').Append('/').Append(elementName).Append('>');
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
