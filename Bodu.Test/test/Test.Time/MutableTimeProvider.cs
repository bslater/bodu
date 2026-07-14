// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Time;

/// <summary>
/// A <see cref="TimeProvider" /> whose current instant is set explicitly, so tests can control freshness, expiry, and
/// on-demand windowing deterministically.
/// </summary>
/// <remarks>
/// This is the shared canonical controllable clock promoted from the per-project copies the Financial and Calendar
/// test suites previously duplicated; it unifies their two API shapes (a settable <see cref="UtcNow" /> property and
/// the <see cref="Advance" />/<see cref="Set" /> methods).
/// </remarks>
public sealed class MutableTimeProvider
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

    /// <summary>
    /// Sets the current instant.
    /// </summary>
    /// <param name="utcNow">The instant to set.</param>
    public void Set(DateTimeOffset utcNow) => UtcNow = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
