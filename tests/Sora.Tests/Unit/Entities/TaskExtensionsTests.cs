using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="TaskExtensions" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class TaskExtensionsTests
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region RunCatch Exception Forwarding

    /// <see cref="TaskExtensions.RunCatch(Task, Action{Exception})" />
    [Fact]
    public async Task RunCatch_Task_ExceptionForwarded()
    {
        Exception?           captured = null;
        TaskCompletionSource signal   = new();

        Task faultedTask = Task.FromException(new InvalidOperationException("test error"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            ex =>
            {
                captured = ex;
                signal.SetResult();
            });

        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.NotNull(captured);
        Assert.Equal("test error", captured.Message);
    }

    /// <see cref="TaskExtensions.RunCatch(ValueTask, Action{Exception})" />
    [Fact]
    public async Task RunCatch_ValueTask_ExceptionForwarded()
    {
        Exception?           captured = null;
        TaskCompletionSource signal   = new();

        ValueTask faultedTask = ValueTask.FromException(new InvalidOperationException("vt error"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            ex =>
            {
                captured = ex;
                signal.SetResult();
            });

        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.NotNull(captured);
        Assert.Equal("vt error", captured.Message);
    }

#endregion

#region RunCatch onError Protection

    /// <see cref="TaskExtensions.RunCatch(Task, Action{Exception})" />
    [Fact]
    public async Task RunCatch_Task_OnErrorThrows_DoesNotCrash()
    {
        TaskCompletionSource signal = new();

        Task faultedTask = Task.FromException(new InvalidOperationException("original"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            _ =>
            {
                signal.SetResult();
                throw new InvalidOperationException("onError itself threw");
            });

        // If onError throwing crashed the process, we'd never reach here
        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.True(signal.Task.IsCompletedSuccessfully);
    }

    /// <see cref="TaskExtensions.RunCatch(ValueTask, Action{Exception})" />
    [Fact]
    public async Task RunCatch_ValueTask_OnErrorThrows_DoesNotCrash()
    {
        TaskCompletionSource signal = new();

        ValueTask faultedTask = ValueTask.FromException(new InvalidOperationException("original"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            _ =>
            {
                signal.SetResult();
                throw new InvalidOperationException("onError itself threw");
            });

        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.True(signal.Task.IsCompletedSuccessfully);
    }

    /// <see cref="TaskExtensions.RunCatch{T}(Task{T}, Action{Exception})" />
    [Fact]
    public async Task RunCatch_TaskOfT_OnErrorThrows_DoesNotCrash()
    {
        TaskCompletionSource signal = new();

        Task<int> faultedTask = Task.FromException<int>(new InvalidOperationException("original"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            (Action<Exception>)(_ =>
            {
                signal.SetResult();
                throw new InvalidOperationException("onError itself threw");
            }));

        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.True(signal.Task.IsCompletedSuccessfully);
    }

    /// <see cref="TaskExtensions.RunCatch{T}(ValueTask{T}, Action{Exception})" />
    [Fact]
    public async Task RunCatch_ValueTaskOfT_OnErrorThrows_DoesNotCrash()
    {
        TaskCompletionSource signal = new();

        ValueTask<int> faultedTask = ValueTask.FromException<int>(new InvalidOperationException("original"));
        Sora.Entities.Utils.TaskExtensions.RunCatch(
            faultedTask,
            (Action<Exception>)(_ =>
            {
                signal.SetResult();
                throw new InvalidOperationException("onError itself threw");
            }));

        await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.True(signal.Task.IsCompletedSuccessfully);
    }

#endregion

#region RunCatch Success Path

    /// <see cref="TaskExtensions.RunCatch(Task, Action{Exception})" />
    [Fact]
    public async Task RunCatch_Task_Success_OnErrorNotCalled()
    {
        bool onErrorCalled = false;

        Task successTask = Task.CompletedTask;
        Sora.Entities.Utils.TaskExtensions.RunCatch(successTask, _ => onErrorCalled = true);

        // Give async void time to complete
        await Task.Delay(50, CT);
        Assert.False(onErrorCalled);
    }

#endregion
}