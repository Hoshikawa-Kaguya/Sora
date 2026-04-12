using Sora.Adapter.Milky.Converter;
using Sora.Adapter.OneBot11.Converter;
using Xunit;

namespace Sora.Tests.Unit;

#region Per-collection timing fixtures

// Each starts/stops a timer via TestTimingStore on Init/Dispose.

/// <summary>Core.Unit timing fixture.</summary>
public sealed class CoreUnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Unit", "Core");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "Core");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Entities.Unit timing fixture.</summary>
public sealed class EntitiesUnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Unit", "Entities");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "Entities");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Command.Unit timing fixture.</summary>
public sealed class CommandUnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Unit", "Command");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "Command");
        return ValueTask.CompletedTask;
    }
}

/// <summary>OneBot11.Unit timing fixture.</summary>
public sealed class OneBot11UnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        OneBot11MapsterConfig.Configure();
        TestTimingStore.StartTimer("Unit", "OneBot11");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "OneBot11");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Milky.Unit timing fixture.</summary>
public sealed class MilkyUnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        MilkyMapsterConfig.Configure();
        TestTimingStore.StartTimer("Unit", "Milky");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "Milky");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Adapters.Unit timing fixture.</summary>
public sealed class AdaptersUnitFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Unit", "Adapters");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Unit", "Adapters");
        return ValueTask.CompletedTask;
    }
}

#endregion

#region Collection Definitions

/// <summary>Core unit test collection.</summary>
[CollectionDefinition("Core.Unit")]
public class CoreUnitCollection : ICollectionFixture<CoreUnitFixture>
{
}

/// <summary>Entities unit test collection.</summary>
[CollectionDefinition("Entities.Unit")]
public class EntitiesUnitCollection : ICollectionFixture<EntitiesUnitFixture>
{
}

/// <summary>Command unit test collection.</summary>
[CollectionDefinition("Command.Unit")]
public class CommandUnitCollection : ICollectionFixture<CommandUnitFixture>
{
}

/// <summary>OneBot11 unit test collection.</summary>
[CollectionDefinition("OneBot11.Unit")]
public class OneBot11UnitCollection : ICollectionFixture<OneBot11UnitFixture>
{
}

/// <summary>Milky unit test collection.</summary>
[CollectionDefinition("Milky.Unit")]
public class MilkyUnitCollection : ICollectionFixture<MilkyUnitFixture>
{
}

/// <summary>Adapters unit test collection.</summary>
[CollectionDefinition("Adapters.Unit")]
public class AdaptersUnitCollection : ICollectionFixture<AdaptersUnitFixture>
{
}

#endregion