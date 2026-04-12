namespace Sora.Entities.Utils;

/// <summary>
///     System-level utility methods for the Sora framework.
/// </summary>
public static class SysUtils
{
    /// <summary>
    ///     Reads the <c>SORA_LOG_LEVEL_OVERRIDE</c> environment variable and converts it
    ///     to the corresponding <see cref="LogLevel" /> value. The comparison is case-insensitive.
    ///     <para>Accepted values (case-insensitive):</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><c>TRACE</c> → <see cref="LogLevel.Trace" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>DEBUG</c> → <see cref="LogLevel.Debug" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>INFO</c>  → <see cref="LogLevel.Information" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>WARN</c>  → <see cref="LogLevel.Warning" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>ERROR</c> → <see cref="LogLevel.Error" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>FATAL</c> → <see cref="LogLevel.Critical" /></description>
    ///         </item>
    ///         <item>
    ///             <description><c>NONE</c>  → <see cref="LogLevel.None" /></description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <returns>
    ///     The parsed <see cref="LogLevel" />, or <see langword="null" /> if the variable is
    ///     not set, empty, or contains an unrecognized value.
    /// </returns>
    public static LogLevel? GetEnvLogLevelOverride()
    {
        string? env = Environment.GetEnvironmentVariable("SORA_LOG_LEVEL_OVERRIDE");
        if (string.IsNullOrEmpty(env))
            return null;

        return env.ToUpper() switch
                   {
                       "TRACE" => LogLevel.Trace,
                       "DEBUG" => LogLevel.Debug,
                       "INFO"  => LogLevel.Information,
                       "WARN"  => LogLevel.Warning,
                       "ERROR" => LogLevel.Error,
                       "FATAL" => LogLevel.Critical,
                       "NONE"  => LogLevel.None,
                       _       => null
                   };
    }
}