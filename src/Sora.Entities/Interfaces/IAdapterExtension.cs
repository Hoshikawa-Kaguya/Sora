namespace Sora.Entities.Interfaces;

/// <summary>
///     Marker interface for adapter extension APIs.
///     Protocol-specific APIs implement this to be accessible via <see cref="IBotApi.GetExtension{T}" />.
/// </summary>
public interface IAdapterExtension;