using System.ComponentModel.DataAnnotations;

namespace RabbitMqSender.DataClasses.Entities
{
    public class PaymentStatus
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Status { get; set; }
    }

}
