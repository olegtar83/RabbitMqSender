using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RabbitMqSender.DataClasses.Entities
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime ReceivedAt { get; set; }
        public required string JsonMessage { get; set; }
        public Guid PaymentStatusId { get; set; }
        [ForeignKey("PaymentStatusId")]
        public virtual required PaymentStatus PaymentStatus { get; set; }
    }
}
