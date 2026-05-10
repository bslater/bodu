// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheEntry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Represents a single entry in the chronological range-resolution cache, carrying the originating rule profile, the materialised
/// base notable date, an optional adjusted form, and the entry's emission qualification state.
/// </summary>
/// <remarks>
/// <para>
/// Entries are mutable on the <see cref="State" /> and <see cref="Adjusted" /> fields so the pipeline can promote a
/// <see cref="NotableDateCacheState.Computed" /> entry to <see cref="NotableDateCacheState.InWindow" /> or
/// <see cref="NotableDateCacheState.Adjusted" /> as more information becomes available during processing.
/// </para>
/// </remarks>
internal sealed class NotableDateCacheEntry
{
	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateCacheEntry" /> class.
	/// </summary>
	/// <param name="profile">The static profile of the originating rule.</param>
	/// <param name="anchorYear">The civil year of the anchor date used to materialise <paramref name="baseNotable" />.</param>
	/// <param name="baseNotable">The materialised base notable date (pre-adjustment).</param>
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
	/// Gets the civil year of the anchor date used to materialise <see cref="BaseNotable" />.
	/// </summary>
	/// <returns>A four-digit civil year.</returns>
	public int AnchorYear { get; }

	/// <summary>
	/// Gets the materialised base notable date (pre-adjustment).
	/// </summary>
	/// <returns>The base <see cref="NotableDate" /> supplied at construction. Never <see langword="null" />.</returns>
	public NotableDate BaseNotable { get; }

	/// <summary>
	/// Gets or sets the materialised observed notable date produced by an observance adjustment, or <see langword="null" /> when no
	/// adjustment has been applied.
	/// </summary>
	/// <returns>The adjusted <see cref="NotableDate" />, or <see langword="null" /> when no adjustment has fired for this entry.</returns>
	public NotableDate? Adjusted { get; set; }

	/// <summary>
	/// Gets or sets the entry's emission qualification state.
	/// </summary>
	/// <returns>One of the defined <see cref="NotableDateCacheState" /> values.</returns>
	public NotableDateCacheState State { get; set; }

	/// <summary>
	/// Gets the originating rule.
	/// </summary>
	/// <returns>The <see cref="NotableDateRule" /> referenced by <see cref="Profile" />. Never <see langword="null" />.</returns>
	public NotableDateRule Rule => Profile.Rule;

	/// <summary>
	/// Gets a value indicating whether the entry should be emitted to the caller in the resolution output.
	/// </summary>
	/// <returns><see langword="true" /> when <see cref="State" /> is <see cref="NotableDateCacheState.InWindow" /> or <see cref="NotableDateCacheState.Adjusted" />; otherwise <see langword="false" />.</returns>
	public bool IsEmissable =>
		State == NotableDateCacheState.InWindow || State == NotableDateCacheState.Adjusted;
}
