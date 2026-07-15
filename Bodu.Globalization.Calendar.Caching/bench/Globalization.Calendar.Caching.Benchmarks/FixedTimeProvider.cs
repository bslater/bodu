// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching.Benchmarks;

/// <summary>
/// A <see cref="TimeProvider" /> pinned to one instant, so benchmark iterations evaluate freshness against a stable
/// clock and never cross a time-to-live boundary mid-run.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    /// <summary>The pinned instant.</summary>
    private readonly DateTimeOffset _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedTimeProvider" /> class.
    /// </summary>
    /// <param name="utcNow">The instant every query returns.</param>
    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
