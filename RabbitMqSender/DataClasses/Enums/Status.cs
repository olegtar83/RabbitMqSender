using System.ComponentModel;

namespace RabbitMqSender.DataClasses.Enums
{
    public enum Status
    {
        [Description("Получен")]
        Received = 1,
        [Description("Передан внешней системе")]
        Sent = 2,
        [Description("Ошибка обработки")]
        Error = 3
    }
}
