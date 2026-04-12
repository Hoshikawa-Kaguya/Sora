using Xunit;

namespace Sora.Tests.Helpers;

/// <summary>
///     Extension methods for <see cref="ApiResult{T}" /> used in functional tests
///     to safely assert success and extract the non-null data payload.
/// </summary>
internal static class ApiResultTestExtensions
{
    /// <summary>
    ///     Asserts that the API result indicates success and that the data payload is not null.
    ///     Returns the non-null data payload for further assertions.
    /// </summary>
    /// <typeparam name="T">The data payload type.</typeparam>
    /// <param name="result">The API result to check.</param>
    /// <returns>The non-null data payload.</returns>
    public static T AssertSuccess<T>(this ApiResult<T> result) where T : class
    {
        Assert.True(result.IsSuccess, $"Expected API success but got {result.Code}: {result.Message}");
        return result.Data ?? throw new InvalidOperationException("Expected non-null Data in successful API result");
    }
}