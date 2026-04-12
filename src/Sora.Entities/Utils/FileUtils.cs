namespace Sora.Entities.Utils;

/// <summary>File utility methods for message segment construction.</summary>
public static class FileUtils
{
    /// <summary>
    ///     Converts a byte array to a <c>base64://</c> URI suitable for resource segment <c>FileUri</c> properties.
    /// </summary>
    /// <param name="data">The raw file bytes.</param>
    /// <returns>A string in the format <c>base64://{base64EncodedContent}</c>.</returns>
    public static string BytesToBase64Uri(ReadOnlySpan<byte> data) => $"base64://{Convert.ToBase64String(data)}";

    /// <summary>
    ///     Asynchronously reads a file and returns a <c>base64://</c> URI suitable for resource segment <c>FileUri</c>
    ///     properties.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A string in the format <c>base64://{base64EncodedContent}</c>.</returns>
    public static async ValueTask<string> FileToBase64UriAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return string.Empty;
        ReadOnlySpan<byte> bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return $"base64://{Convert.ToBase64String(bytes)}";
    }
}