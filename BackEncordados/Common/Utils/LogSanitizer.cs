namespace BackEncordados.Common.Utils;

public static class LogSanitizer
{
    public static string SanitizeForLog(this string? value)
    {
        if (value is null) return string.Empty;
        return value
            .Replace("\r\n", " ")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}
