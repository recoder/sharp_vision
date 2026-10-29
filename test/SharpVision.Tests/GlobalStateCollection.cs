using Xunit;

namespace SharpVision.Tests;

/// <summary>
/// Tests that modify process-wide state (code page translation, screen size, ...) go in
/// this collection so they never run in parallel with other tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalStateCollection
{
    public const string Name = "Global state";
}
