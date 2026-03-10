namespace RabbitMqSender.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue? GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey: notnull
        {
            return dictionary.TryGetValue(key, out var value) ? value : default;
        }
    }
}
