// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateHistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Declares how far back in time a provider can serve exchange rates, as one of three shapes: an unbounded archive, a
/// rolling window of the most recent days, or a fixed earliest calendar date.
/// </summary>
/// <remarks>
/// <para>
/// A provider advertises its history depth through this value so a caller can know, before requesting an old date,
/// whether the provider can satisfy it. A feed that publishes only the recent past (for example, an anonymous endpoint
/// capped to roughly the last six months) declares a <see cref="RateHistoryAvailabilityKind.Rolling" /> window; a feed
/// with a fixed inception date declares <see cref="RateHistoryAvailabilityKind.Since" />; a feed with no known floor
/// declares <see cref="RateHistoryAvailabilityKind.Unbounded" />.
/// </para>
/// <para>
/// The value is advisory: it describes the provider's published coverage, not a guarantee that every day in range
/// carries an observation (weekends and holidays may still be absent). It bounds only the earliest date worth
/// requesting.
/// </para>
/// </remarks>
public readonly record struct RateHistoryAvailability
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateHistoryAvailability" /> class.
    /// </summary>
    /// <param name="kind">The shape of the availability bound.</param>
    /// <param name="windowDays">
    /// The rolling window length in days; meaningful only when <paramref name="kind" /> is
    /// <see cref="RateHistoryAvailabilityKind.Rolling" />.
    /// </param>
    /// <param name="earliestDate">
    /// The fixed earliest date; meaningful only when <paramref name="kind" /> is
    /// <see cref="RateHistoryAvailabilityKind.Since" />.
    /// </param>
    private RateHistoryAvailability(RateHistoryAvailabilityKind kind, int windowDays, DateOnly earliestDate)
    {
        Kind = kind;
        WindowDays = windowDays;
        EarliestDate = earliestDate;
    }

    /// <summary>
    /// Gets the shape of the availability bound.
    /// </summary>
    /// <value>The availability kind.</value>
    public RateHistoryAvailabilityKind Kind { get; }

    /// <summary>
    /// Gets the rolling window length, in days, measured back from the current date.
    /// </summary>
    /// <value>
    /// The window length in days when <see cref="Kind" /> is <see cref="RateHistoryAvailabilityKind.Rolling" />;
    /// otherwise zero.
    /// </value>
    public int WindowDays { get; }

    /// <summary>
    /// Gets the fixed earliest date on or after which the provider serves rates.
    /// </summary>
    /// <value>
    /// The earliest date when <see cref="Kind" /> is <see cref="RateHistoryAvailabilityKind.Since" />; otherwise the
    /// default date.
    /// </value>
    public DateOnly EarliestDate { get; }

    /// <summary>
    /// Gets an availability that advertises no known lower bound on the dates the provider can serve.
    /// </summary>
    /// <value>An unbounded availability.</value>
    public static RateHistoryAvailability Unbounded =>
        new(RateHistoryAvailabilityKind.Unbounded, 0, default);

    /// <summary>
    /// Creates an availability describing a rolling window of the most recent <paramref name="days" /> days.
    /// </summary>
    /// <param name="days">The window length in days.</param>
    /// <returns>A rolling availability spanning the last <paramref name="days" /> days.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="days" /> is less than or equal to zero.
    /// </exception>
    public static RateHistoryAvailability RollingDays(int days)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(days, 0);

        return new(RateHistoryAvailabilityKind.Rolling, days, default);
    }

    /// <summary>
    /// Creates an availability describing a fixed earliest date on or after which the provider serves rates.
    /// </summary>
    /// <param name="earliest">The earliest available date.</param>
    /// <returns>A fixed-floor availability starting at <paramref name="earliest" />.</returns>
    public static RateHistoryAvailability Since(DateOnly earliest) =>
        new(RateHistoryAvailabilityKind.Since, 0, earliest);

    /// <summary>
    /// Resolves the earliest date the provider can serve, evaluated against a reference date.
    /// </summary>
    /// <param name="asOf">The reference date from which a rolling window is measured.</param>
    /// <returns>
    /// The earliest available date, or <see langword="null" /> when the availability is
    /// <see cref="RateHistoryAvailabilityKind.Unbounded" />.
    /// </returns>
    public DateOnly? GetEarliestAvailable(DateOnly asOf) =>
        Kind switch
        {
            RateHistoryAvailabilityKind.Rolling => asOf.AddDays(-WindowDays),
            RateHistoryAvailabilityKind.Since => EarliestDate,
            _ => null,
        };

    /// <summary>
    /// Reports whether <paramref name="date" /> falls within the provider's advertised history, evaluated against a
    /// reference date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="asOf">The reference date from which a rolling window is measured.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="date" /> is on or after the earliest available date, or when the
    /// availability is unbounded; otherwise <see langword="false" />.
    /// </returns>
    public bool IsAvailable(DateOnly date, DateOnly asOf)
    {
        DateOnly? earliest = GetEarliestAvailable(asOf);
        return earliest is null || date >= earliest.Value;
    }
}
