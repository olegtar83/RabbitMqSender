using RabbitMqSender.Database.Abstractions;

namespace RabbitMqSender.Database
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IApplicationDbContext>>();

            try
            {
                logger.LogInformation("Starting database migration...");
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database migration completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database");
                throw;
            }
        }
    }
}