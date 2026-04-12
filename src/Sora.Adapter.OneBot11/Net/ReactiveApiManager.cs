using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using Sora.Adapter.OneBot11.Models;

namespace Sora.Adapter.OneBot11.Net;

/// <summary>Manages OB11 API request/response matching via Reactive echo GUIDs.</summary>
internal sealed class ReactiveApiManager : IDisposable
{
    private readonly Lazy<ILogger>              _loggerLazy = new(SoraLogger.CreateLogger<ReactiveApiManager>);
    private          ILogger                    _logger => _loggerLazy.Value;
    private readonly Subject<OneBotApiResponse> _responseSubject = new();
    private readonly TimeSpan                   _timeout;

    /// <summary>Initializes a new instance of the <see cref="ReactiveApiManager" /> class.</summary>
    /// <param name="timeout">The timeout for API response waiting.</param>
    public ReactiveApiManager(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    /// <summary>Feed an incoming API response into the reactive pipeline.</summary>
    /// <param name="response">The API response to feed into the pipeline.</param>
    public void HandleResponse(OneBotApiResponse response)
    {
        _logger.LogInformation(
            "OB11 Api response received [echo: {Echo}] retCode={RetCode} status={Status}",
            response.Echo,
            response.RetCode,
            response.Status);
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("OB11 Api response payload: {Json}", JsonConvert.SerializeObject(response));
        _responseSubject.OnNext(response);
    }

    /// <summary>Send an API request and wait for matching response.</summary>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <param name="sendFunc">The function to send the serialized request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matched API response.</returns>
    public async ValueTask<OneBotApiResponse> SendRequestAsync(
        string                  action,
        object?                 parameters,
        Func<string, ValueTask> sendFunc,
        CancellationToken       ct = default)
    {
        string echo = Guid.NewGuid().ToString();
        OneBotApiRequest request = new()
            {
                Action = action,
                Params = parameters ?? new object(),
                Echo   = echo
            };

        _logger.LogDebug("OB11 Api call: {Action} [echo: {Echo}]", action, echo);
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("OB11 Api call payload: [{Action}]{@Para}", action, parameters);

        string json = JsonConvert.SerializeObject(request);
        Task<OneBotApiResponse> responseTask = _responseSubject
                                               .Where(r => r.Echo == echo)
                                               .Timeout(_timeout)
                                               .FirstAsync()
                                               .ToTask(ct);

        try
        {
            await sendFunc(json);
            OneBotApiResponse response = await responseTask;
            _logger.LogDebug(
                "OB11 Api request completed: {Action} [echo: {Echo}] retCode={RetCode} status={Status}",
                action,
                echo,
                response.RetCode,
                response.Status);
            return response;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "OB11 Api request timed out: {Action} [echo: {Echo}] after {TimeoutMs} ms",
                action,
                echo,
                _timeout.TotalMilliseconds);
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("OB11 API request canceled: {Action} [echo: {Echo}]", action, echo);
            throw;
        }
    }

    /// <summary>Disposes the reactive pipeline.</summary>
    public void Dispose()
    {
        try
        {
            _responseSubject.OnCompleted();
        }
        catch (ObjectDisposedException)
        {
            /* Subject already disposed */
        }

        _responseSubject.Dispose();
    }
}