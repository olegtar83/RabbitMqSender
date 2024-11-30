using System.Xml.Serialization;

namespace RabbitMqSender.DataClasses
{
    [XmlRoot("invoice_payment")]
    public class InvoicePayment
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("debit")]
        public string Debit { get; set; }

        [XmlElement("credit")]
        public string Credit { get; set; }

        [XmlElement("amount")]
        public decimal Amount { get; set; }

        [XmlElement("currency")]
        public string Currency { get; set; }

        [XmlElement("details")]
        public string Details { get; set; }

        [XmlElement("pack")]
        public string Pack { get; set; }
    }
}
