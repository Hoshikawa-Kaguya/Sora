namespace Sora.Entities.Utils;

/// <summary>Extension methods for string parsing.</summary>
public static class StringUtils
{
    /// <summary>Parses the string as an <see cref="int" />, returning <paramref name="defaultValue" /> on failure.</summary>
    public static int ToIntOrDefault(this string str, int defaultValue = 0) => int.TryParse(str, out int val) ? val : defaultValue;

    /// <summary>Parses the string as a <see cref="long" />, returning <paramref name="defaultValue" /> on failure.</summary>
    public static long ToLongOrDefault(this string str, int defaultValue = 0) => long.TryParse(str, out long val) ? val : defaultValue;
}