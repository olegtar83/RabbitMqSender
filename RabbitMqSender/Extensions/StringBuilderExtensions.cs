using System.Text;

namespace RabbitMqSender.Extensions
{
    public static class StringBuilderExtensions
    {
        public static void AppendFormattedValue<T>(this StringBuilder sb, T value, string elementName)
            where T : ISpanFormattable
        {
            sb.Append('<').Append(elementName).Append('>');

            Span<char> buffer = stackalloc char[64];
            if (value.TryFormat(buffer, out int charsWritten, default, null))
            {
                sb.Append(buffer[..charsWritten]);
            }
            else
            {
                sb.Append(value.ToString());
            }

            sb.Append('<').Append('/').Append(elementName).Append('>');
        }
    }
}

