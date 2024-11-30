﻿using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMqSender.Database.Abstractions;
using RabbitMqSender.DataClasses;
using RabbitMqSender.DataClasses.Entities;
using RabbitMqSender.DataClasses.Enums;
using RabbitMqSender.Extensions;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

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

            var payment = new Payment()
            {
                JsonMessage = JsonSerializer.Serialize(context.Message),
                ReceivedAt = DateTime.UtcNow,
                PaymentStatus = statuses.First(x =>
                    x.Status == Status.Received.GetDescription())
            };

            _logger.LogInformation($"Received {JsonSerializer.Serialize(context.Message)}");

            var xmlString = ConvertToXml(context.Message);

            _logger.LogInformation($"Converted {xmlString}");

            try
            {
                var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");

                var response = await _client.PostAsync("", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    payment.PaymentStatus = statuses.First(x =>
                        x.Status == Status.Error.GetDescription());
                }
                else
                {
                    payment.PaymentStatus = statuses.First(x =>
                       x.Status == Status.Sent.GetDescription());
                }
            }
            catch (HttpRequestException httpEx)
            {
                payment.PaymentStatus = statuses.First(x =>
                        x.Status == Status.Error.GetDescription());
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

            var serializer = new XmlSerializer(typeof(InvoicePayment));
            string xmlString;
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = System.Xml.XmlWriter.Create(stringWriter))
                {
                    serializer.Serialize(xmlWriter, invoicePayment);
                    xmlString = stringWriter.ToString();
                }
            }
            return xmlString;
        }
    }
}