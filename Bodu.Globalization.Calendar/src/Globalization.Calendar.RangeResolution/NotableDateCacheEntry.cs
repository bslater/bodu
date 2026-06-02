// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Represents a single entry in the chronological range-resolution cache, carrying the originating rule profile, the
/// materialized base notable date, an optional adjusted form, and the entry's emission qualification state.
/// </summary>
/// <remarks>
/// <para>
/// Entries are mutable on the <see cref="Adjusted" /> and <see cref="AdjustmentActivated" /> fields so the pipeline can
/// record an observance adjustment as it is applied. The emission stage recomputes window intersection from the base
/// and adjusted dates, so the entry's role (<see cref="NotableDateCacheState" />) only distinguishes a real occurrence
/// from pure adjustment context.
/// </para>
/// </remarks>
internal sealed class NotableDateCacheEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateCacheEntry" /> class.
    /// </summary>
    /// <param name="profile">The static profile of the originating rule.</param>
    /// <param name="anchorYear">
    /// The civil year of the anchor date used to materialize <paramref name="baseNotable" />.
    /// </param>
    /// <param name="baseNotable">The materialized base notable date (pre-adjustment).</param>
    /// <param name="state">The initial qualification state.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="profile" /> or <paramref name="baseNotable" /> is <see langword="null" />.
    /// </exception>
    public NotableDateCacheEntry(
        RuleStaticProfile profile,
        int anchorYear,
        NotableDate baseNotable,
        NotableDateCacheState state)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        AnchorYear = anchorYear;
        BaseNotable = baseNotable ?? throw new ArgumentNullException(nameof(baseNotable));
        State = state;
    }

    /// <summary>
    /// Gets the static profile of the originating rule.
    /// </summary>
    /// <returns>The <see cref="RuleStaticProfile" /> supplied at construction. Never <see langword="null" />.</returns>
    public RuleStaticProfile Profile { get; }

    /// <summary>
    /// Gets the civil year of the anchor date used to materialize <see cref="BaseNotable" />.
    /// </summary>
    /// <returns>A four-digit civil year.</returns>
    public int AnchorYear { get; }

    /// <summary>
    /// Gets the materialized base notable date (pre-adjustment).
    /// </summary>
    /// <returns>The base <see cref="NotableDate" /> supplied at construction. Never <see langword="null" />.</returns>
    public NotableDate BaseNotable { get; }

    /// <summary>
    /// Gets or sets the materialized observed notable date produced by an observance adjustment, or
    /// <see langword="null" /> when no adjustment has been applied.
    /// </summary>
    /// <returns>
    /// The adjusted <see cref="NotableDate" />, or <see langword="null" /> when no adjustment has fired for this entry.
    /// </returns>
    public NotableDate? Adjusted { get; set; }

    /// <summary>
    /// Gets or sets the entry's materialization role.
    /// </summary>
    /// <returns>One of the defined <see cref="NotableDateCacheState" /> values.</returns>
    public NotableDateCacheState State { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an observance adjustment activated for this entry and moved its date.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when an adjustment fired and produced <see cref="Adjusted" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool AdjustmentActivated { get; set; }

    /// <summary>
    /// Gets the originating rule.
    /// </summary>
    /// <returns>
    /// The <see cref="NotableDateRule" /> referenced by <see cref="Profile" />. Never <see langword="null" />.
    /// </returns>
    public NotableDateRule Rule => Profile.Rule;

    /// <summary>
    /// Gets a value indicating whether the entry represents a real occurrence eligible for emission consideration.
    /// </summary>
    /// <remarks>
    /// Eligibility does not by itself emit the entry: the emission stage still intersects the base and adjusted dates
    /// with the requested window under the active <see cref="ObservedDateMode" />.
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> when <see cref="State" /> is <see cref="NotableDateCacheState.Candidate" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool IsEmissable =>
        State is NotableDateCacheState.Candidate;
}
