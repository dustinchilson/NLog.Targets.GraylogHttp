namespace NLog.Targets.GraylogHttp
{
    internal static class StringExt
    {
        internal static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength);
        }
    }
}