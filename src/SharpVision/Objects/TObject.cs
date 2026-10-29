// Ported from include/tvision/tvobjs.h and source/tvision/tobject.cpp (class TObject).

namespace SharpVision.Objects;

/// <summary>
/// The root of the Turbo Vision object hierarchy.
/// </summary>
/// <remarks>
/// Memory is garbage collected, but teardown still follows upstream: call
/// <see cref="Destroy"/>, which runs <see cref="ShutDown"/> so the object can release
/// links to other objects (owners, peers) before it is dropped.
/// </remarks>
public class TObject
{
    /// <summary>Shuts down <paramref name="o"/>, if not null. The C# counterpart of upstream's <c>destroy</c>.</summary>
    public static void Destroy(TObject? o) => o?.ShutDown();

    /// <summary>Releases links to other objects. Called by <see cref="Destroy"/>.</summary>
    public virtual void ShutDown()
    {
    }
}
