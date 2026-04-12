using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Sora.Adapter.Milky.Models;

namespace Sora.Adapter.Milky.Net;

/// <summary>HTTP client for Milky API calls.</summary>
internal sealed class MilkyHttpApiClient : IDisposable
{
    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();
    private readonly        string         _baseUrl;
    private readonly        HttpClient     _httpClient;
    private readonly        Lazy<ILogger>  _loggerLazy = new(SoraLogger.CreateLogger<MilkyHttpApiClient>);
    private                 ILogger        _logger => _loggerLazy.Value;

    /// <summary>Initializes a new instance of the <see cref="MilkyHttpApiClient" /> class.</summary>
    /// <param name="config">The Milky adapter configuration.</param>
    public MilkyHttpApiClient(MilkyConfig config)
    {
        _baseUrl    = config.GetApiBaseUrl();
        _httpClient = new HttpClient(config.CreateHttpHandler(), true) { Timeout = config.ApiTimeout };

        if (!string.IsNullOrEmpty(config.AccessToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
    }

    /// <summary>Calls a Milky API endpoint.</summary>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters to serialize as JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized API response.</returns>
    public async ValueTask<MilkyApiResponse> CallApiAsync(
        string            action,
        object?           parameters = null,
        CancellationToken ct         = default)
    {
        _logger.LogDebug("Milky Api call: {Action}", action);
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogTrace("Milky Api call payload: [{Action}]{@Para}", action, parameters);
        string url = $"{_baseUrl}/{action}";
        string json = parameters is not null
            ? JsonConvert.SerializeObject(parameters)
            : "{}";
        StringContent content = new(json, Encoding.UTF8, "application/json");
        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsync(url, content, ct);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    Stream                     responseStream = await response.Content.ReadAsStreamAsync(ct);
                    using StreamReader         sr             = new(responseStream);
                    await using JsonTextReader reader         = new(sr);
                    MilkyApiResponse apiResponse = Serializer.Deserialize<MilkyApiResponse>(reader)
                                                   ?? new MilkyApiResponse
                                                       {
                                                           Status  = "failed",
                                                           RetCode = (int)ApiStatusCode.InternalError,
                                                           Message = "Invalid response"
                                                       };
                    _logger.LogDebug(
                        "Milky Api call completed: [{Action}] status={Status} retCode={RetCode}",
                        action,
                        apiResponse.Status,
                        apiResponse.RetCode);
                    if (apiResponse.RetCode == 0) return apiResponse;

                    //api server fall back
                    _logger.LogError(
                        "Milky Api internal server error for [{Action}] Http return OK, but code={retCode}",
                        action,
                        apiResponse.RetCode);
                    return new MilkyApiResponse
                            { Status = "failed", RetCode = apiResponse.RetCode };
                }
                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Milky API call unauthorized: [{Action}]", action);
                    return new MilkyApiResponse { Status = "failed", RetCode = (int)response.StatusCode, Message = "Unauthorized" };
                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Milky API endpoint not found: [{Action}]", action);
                    return new MilkyApiResponse { Status = "failed", RetCode = (int)response.StatusCode, Message = "API not found" };
                case HttpStatusCode.UnsupportedMediaType:
                    _logger.LogWarning("Milky API rejected content type for [{Action}]", action);
                    return new MilkyApiResponse
                            { Status = "failed", RetCode = (int)response.StatusCode, Message = "Unsupported Content-Type" };
                case HttpStatusCode.InternalServerError:
                    _logger.LogError("Milky API internal server error for [{Action}]", action);
                    return new MilkyApiResponse
                            { Status = "failed", RetCode = (int)response.StatusCode, Message = "Internal Server Error" };
                default:
                    _logger.LogError(
                        "Milky API call [{Action}] return unknown status code: [{code}]{intCode}",
                        action,
                        response.StatusCode,
                        (int)response.StatusCode);
                    return new MilkyApiResponse
                            { Status = "failed", RetCode = (int)response.StatusCode, Message = "Unknown response" };
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Milky API call timed out: {Action}", action);
            return new MilkyApiResponse { Status = "failed", RetCode = (int)ApiStatusCode.Timeout, Message = "Request timed out" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Milky API call failed: {Action}", action);
            return new MilkyApiResponse { Status = "failed", RetCode = (int)ApiStatusCode.InternalError, Message = ex.Message };
        }
    }

    /// <summary>Disposes the underlying HTTP client.</summary>
    public void Dispose() => _httpClient.Dispose();
}