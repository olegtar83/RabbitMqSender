using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RabbitMqSender.DataClasses.Entities;

namespace RabbitMqSender.Database.Abstractions
{
    public interface IApplicationDbContext
    {
        DatabaseFacade Database { get; }
        DbSet<Payment> Payments { get; set; }
        DbSet<PaymentStatus> PaymentStatus { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}
