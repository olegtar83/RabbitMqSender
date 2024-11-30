using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMqSender.Database;
using RabbitMqSender.Database.Abstractions;
using System.Net.Http.Headers;
using System.Reflection;

namespace RabbitMqSender
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddMassTransit(cfg =>
            {
                var entryAssembly = Assembly.GetExecutingAssembly();

                cfg.SetKebabCaseEndpointNameFormatter();
                cfg.AddConsumers(entryAssembly);
                cfg.UsingRabbitMq((context, busFactoryConfigurator) =>
                {
                    busFactoryConfigurator.Host(Environment.GetEnvironmentVariable("RabbitMqSettings:Host"), "/", h => { });

                    busFactoryConfigurator.ConfigureEndpoints(context);
                });
            });
            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("DatabaseSettings:ConnStr"), builder =>
                {
                    builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    builder.EnableRetryOnFailure(3);
                });
            });

            services.AddHttpClient("InvoiceClient", client =>
            {
                client.BaseAddress = new Uri("https://somesite/api/v1/invoice");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            });

            return services;
        }
    }
}
