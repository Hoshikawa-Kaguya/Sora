namespace Sora.Core.Types;

/// <summary>
///     Result of an API call without data payload.
/// </summary>
/// <param name="Code">Status code indicating success or failure.</param>
/// <param name="Message">status message.</param>
public readonly record struct ApiResult(ApiStatusCode Code, string Message)
{
    /// <summary>Gets whether this result indicates success.</summary>
    public bool IsSuccess => Code == ApiStatusCode.Ok;

    /// <summary>Creates a successful result.</summary>
    /// <param name="message">Optional status message (defaults to "ok").</param>
    /// <returns>A new <see cref="ApiResult" /> with <see cref="ApiStatusCode.Ok" />.</returns>
    public static ApiResult Ok(string message = "ok") => new(ApiStatusCode.Ok, message);

    /// <summary>Creates a failed result.</summary>
    /// <param name="code">The failure status code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A new <see cref="ApiResult" /> with the specified failure code.</returns>
    public static ApiResult Fail(ApiStatusCode code, string message) => new(code, message);
}

/// <summary>
///     Result of an API call with a typed data payload.
/// </summary>
/// <typeparam name="T">Type of the data payload.</typeparam>
/// <param name="Code">Status code indicating success or failure.</param>
/// <param name="Message">Status message.</param>
/// <param name="Data">Response data payload (default if failed).</param>
public readonly record struct ApiResult<T>(ApiStatusCode Code, string Message, T? Data)
{
    /// <summary>Gets whether this result indicates success.</summary>
    public bool IsSuccess => Code == ApiStatusCode.Ok;

    /// <summary>Creates a successful result with data.</summary>
    /// <param name="data">The response data payload.</param>
    /// <param name="message">Optional status message (defaults to "ok").</param>
    /// <returns>A new <see cref="ApiResult{T}" /> with <see cref="ApiStatusCode.Ok" /> and the given data.</returns>
    public static ApiResult<T> Ok(T data, string message = "ok") => new(ApiStatusCode.Ok, message, data);

    /// <summary>Creates a failed result.</summary>
    /// <param name="code">The failure status code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A new <see cref="ApiResult{T}" /> with the specified failure code and default data.</returns>
    public static ApiResult<T> Fail(ApiStatusCode code, string message) => new(code, message, default);
}

/// <summary>
///     Result of a send message API call.
/// </summary>
/// <param name="Code">Status code indicating success or failure.</param>
/// <param name="MessageId">The ID of the sent message (default if failed).</param>
/// <param name="ErrorMessage">Error message when the operation fails.</param>
public readonly record struct SendMessageResult(ApiStatusCode Code, MessageId MessageId, string ErrorMessage)
{
    /// <summary>Gets whether this result indicates success.</summary>
    public bool IsSuccess => Code == ApiStatusCode.Ok;

    /// <summary>Creates a successful result.</summary>
    /// <param name="messageId">The ID of the sent message.</param>
    /// <returns>A new <see cref="SendMessageResult" /> with <see cref="ApiStatusCode.Ok" />.</returns>
    public static SendMessageResult Ok(MessageId messageId) => new(ApiStatusCode.Ok, messageId, string.Empty);

    /// <summary>Creates a failed result.</summary>
    /// <param name="code">The failure status code.</param>
    /// <param name="err">Error message</param>
    /// <returns>A new <see cref="SendMessageResult" /> with the specified failure code.</returns>
    public static SendMessageResult Fail(ApiStatusCode code, string err) => new(code, default, err);
}