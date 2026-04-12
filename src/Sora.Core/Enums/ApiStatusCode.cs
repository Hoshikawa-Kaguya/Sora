namespace Sora.Core.Enums;

/// <summary>
///     Status codes for API call results, organized into four zones:
///     <list type="bullet">
///         <item>Negative (&lt; 0): Protocol endpoint errors from the protocol implementation (e.g., LLBot)</item>
///         <item>Zero (0): Success</item>
///         <item>HTTP (1–999): HTTP transport-level errors</item>
///         <item>Framework (≥ 10000): Framework or adapter internal errors</item>
///     </list>
/// </summary>
public enum ApiStatusCode
{
    // === Protocol endpoint errors (negative, from protocol implementation) ===

    /// <summary>Protocol internal error.</summary>
    ProtocolError = -500,

    /// <summary>Resource not found by the protocol (e.g., user or group does not exist).</summary>
    ProtocolNotFound = -404,

    /// <summary>Access denied by the protocol (e.g., bot is not logged in).</summary>
    ProtocolForbidden = -403,

    /// <summary>Invalid parameters sent to the protocol endpoint.</summary>
    ProtocolBadRequest = -400,

    // === Success ===

    /// <summary>API call succeeded.</summary>
    Ok = 0,

    // === HTTP transport errors ===

    /// <summary>HTTP 401 — Authentication credentials missing or invalid.</summary>
    Unauthorized = 401,

    /// <summary>HTTP 403 — Access forbidden.</summary>
    Forbidden = 403,

    /// <summary>HTTP 404 — API endpoint not found.</summary>
    NotFound = 404,

    /// <summary>HTTP 415 — Unsupported Content-Type.</summary>
    UnsupportedMediaType = 415,

    /// <summary>HTTP 500 — Server error.</summary>
    Error = 500,

    // === Framework internal errors (≥ 10000) ===

    /// <summary>Unknown error occurred.</summary>
    Unknown = 10000,

    /// <summary>API call timed out.</summary>
    Timeout = 10001,

    /// <summary>An unhandled exception occurred within the framework or adapter.</summary>
    InternalError = 10002,

    /// <summary>The message content is invalid or empty after conversion.</summary>
    InvalidMessage = 10003,

    /// <summary>API call failed.</summary>
    Failed = 10004
}