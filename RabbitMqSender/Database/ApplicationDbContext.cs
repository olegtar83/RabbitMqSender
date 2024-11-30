using Microsoft.EntityFrameworkCore;
using RabbitMqSender.Database.Abstractions;
using RabbitMqSender.DataClasses.Entities;
using RabbitMqSender.DataClasses.Enums;
using RabbitMqSender.Extensions;

namespace RabbitMqSender.Database
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public virtual DbSet<Payment> Payments { get; set; }
        public virtual DbSet<PaymentStatus> PaymentStatus { get; set; }

        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Payment>()
                .HasOne(e => e.PaymentStatus)
                .WithMany()
                .HasForeignKey(e => e.PaymentStatusId);

            modelBuilder.Entity<PaymentStatus>().HasData(
                new PaymentStatus { Id = Guid.NewGuid(), Status = Status.Received.GetDescription() },
                new PaymentStatus { Id = Guid.NewGuid(), Status = Status.Sent.GetDescription() },
                new PaymentStatus { Id = Guid.NewGuid(), Status = Status.Error.GetDescription() }
            );

            modelBuilder.Entity<Payment>()
                .HasIndex(e => e.PaymentStatusId)
                .HasDatabaseName("IX_Payment_PaymentStatusId");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql();
            base.OnConfiguring(optionsBuilder);
        }
    }
}
