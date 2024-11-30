using System.Xml.Serialization;

namespace RabbitMqSender.Extensions
{
    public static class ObjectExtensions
    {
        static public string SeriallizeToXml<TObject>(this object objectValue)
        {
            var serializer = new XmlSerializer(typeof(TObject));
            using var stringWriter = new StringWriter();
            using var xmlWriter = System.Xml.XmlWriter.Create(stringWriter);
            serializer.Serialize(xmlWriter, objectValue);
            return stringWriter.ToString();
        }
    }
}
