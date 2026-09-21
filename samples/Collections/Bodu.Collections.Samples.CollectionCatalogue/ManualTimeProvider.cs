// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ManualTimeProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Samples.CollectionCatalogue;

/// <summary>
/// A <see cref="TimeProvider" /> whose clock only moves when the caller advances it.
/// </summary>
/// <remarks>
/// Time-based cache expiry is untestable and undemonstrable against the wall clock: a sample would have to sleep,
/// which makes it slow, and it would still be at the mercy of scheduling. Because
/// <see cref="Bodu.Collections.Generic.EvictingDictionaryExpiration" /> takes the
/// <see cref="TimeProvider" /> that drives every clock read, the sample can step time forward by an exact amount and
/// print an outcome that is identical on every run and every machine. Production code leaves the default
/// (<see cref="TimeProvider.System" />) in place.
/// </remarks>
public sealed class ManualTimeProvider : TimeProvider
{
    /// <summary>The current instant reported by this provider.</summary>
    private DateTimeOffset _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualTimeProvider" /> class starting at the specified instant.
    /// </summary>
    /// <param name="start">The instant the clock initially reports.</param>
    public ManualTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    /// <summary>
    /// Returns the instant the clock currently reports.
    /// </summary>
    /// <returns>The current instant, which changes only through <see cref="Advance(TimeSpan)" />.</returns>
    public override DateTimeOffset GetUtcNow() =>
        _utcNow;

    /// <summary>
    /// Moves the clock forward by the specified amount.
    /// </summary>
    /// <param name="delta">The amount of time to advance.</param>
    public void Advance(TimeSpan delta) =>
        _utcNow += delta;
}
