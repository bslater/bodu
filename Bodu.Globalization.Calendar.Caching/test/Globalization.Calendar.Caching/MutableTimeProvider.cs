// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// A <see cref="TimeProvider" /> whose current instant is set explicitly, so freshness and time-to-live behaviour can be
/// exercised deterministically.
/// </summary>
internal sealed class MutableTimeProvider : TimeProvider
{
    /// <summary>The current instant returned by <see cref="GetUtcNow" />.</summary>
    private DateTimeOffset _utcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableTimeProvider" /> class.
    /// </summary>
    /// <param name="utcNow">The initial instant.</param>
    public MutableTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>
    /// Advances the current instant by the supplied duration.
    /// </summary>
    /// <param name="delta">The duration to advance by.</param>
    public void Advance(TimeSpan delta) => _utcNow += delta;

    /// <summary>
    /// Sets the current instant.
    /// </summary>
    /// <param name="utcNow">The instant to set.</param>
    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;
}
