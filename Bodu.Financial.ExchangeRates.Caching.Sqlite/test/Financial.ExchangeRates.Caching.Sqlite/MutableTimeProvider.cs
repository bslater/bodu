// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// A <see cref="TimeProvider" /> whose current instant is set explicitly, so tests can control the degradation-warning
/// cooldown deterministically.
/// </summary>
internal sealed class MutableTimeProvider
    : TimeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutableTimeProvider" /> class.
    /// </summary>
    /// <param name="now">The initial instant returned by <see cref="GetUtcNow" />.</param>
    public MutableTimeProvider(DateTimeOffset now) => UtcNow = now;

    /// <summary>
    /// Gets or sets the instant returned by <see cref="GetUtcNow" />.
    /// </summary>
    /// <value>The current instant.</value>
    public DateTimeOffset UtcNow { get; set; }

    /// <summary>
    /// Advances the current instant by the supplied amount.
    /// </summary>
    /// <param name="delta">The amount to advance.</param>
    public void Advance(TimeSpan delta) => UtcNow += delta;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
