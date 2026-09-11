using Sora.Adapter.Milky.Converter;
using Xunit;

namespace Sora.Tests.Unit;

#region Protocol Mapping Fixtures

/// <summary>Initializes mapping for Milky unit tests.</summary>
public sealed class MilkyUnitFixture
{
    /// <summary>Configures the adapter mapping used by this collection.</summary>
    public MilkyUnitFixture()
    {
        MilkyMapsterConfig.Configure();
    }
}

#endregion

#region Collection Definitions

/// <summary>Core unit test collection.</summary>
[CollectionDefinition("Core.Unit")]
public class CoreUnitCollection
{
}

/// <summary>Entities unit test collection.</summary>
[CollectionDefinition("Entities.Unit")]
public class EntitiesUnitCollection
{
}

/// <summary>Command unit test collection.</summary>
[CollectionDefinition("Command.Unit")]
public class CommandUnitCollection
{
}

/// <summary>Milky unit test collection.</summary>
[CollectionDefinition("Milky.Unit")]
public class MilkyUnitCollection : ICollectionFixture<MilkyUnitFixture>
{
}

/// <summary>Adapters unit test collection.</summary>
[CollectionDefinition("Adapters.Unit")]
public class AdaptersUnitCollection
{
}

#endregion