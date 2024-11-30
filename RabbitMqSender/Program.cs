using MassTransit;
using Microsoft.OpenApi.Models;
using RabbitMqSender;
using RabbitMqSender.Database;
using RabbitMqSender.DataClasses;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Logs:Host")!)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<AdditionalPropertiesSchemaFilter>();
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
builder.Services.AddInfrastructure();

var app = builder.Build();

await DatabaseInitializer.InitializeDatabase(app);

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.UseHttpsRedirection();

app.MapPost("/sendPayment", async (IPublishEndpoint publishEndpoint, PaymentRequest paymentRequest, CancellationToken cancellationToken) =>
{
    try
    {
        await publishEndpoint.Publish(paymentRequest, cancellationToken);
        Log.Information("Published message: {Message}", JsonSerializer.Serialize(paymentRequest));

        return Results.Ok(new { message = "Payment request sent successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("SendPayment")
.WithOpenApi();

app.Run();


public class AdditionalPropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.AdditionalPropertiesAllowed = false;
    }
}