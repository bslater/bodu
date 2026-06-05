// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCollisionContext.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Carries the inputs an <see cref="INotableDateCollisionResolver" /> needs to arbitrate the notable dates that apply to
/// a single calendar day: the day itself, the overlapping occurrences, and their provenance.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Overlapping" /> and <see cref="Provenances" /> are index-aligned: <c>Provenances[i]</c> is the layer that
/// produced <c>Overlapping[i]</c>. Each occurrence also carries its own <see cref="NotableDate.Priority" /> and
/// <see cref="NotableDate.Category" />, so a resolver has everything required to apply the documented precedence
/// (provenance, then priority, then category, then name).
/// </para>
/// </remarks>
public sealed class NotableDateCollisionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateCollisionContext" /> class.
    /// </summary>
    /// <param name="day">The calendar day shared by the overlapping occurrences.</param>
    /// <param name="overlapping">The notable dates that apply to <paramref name="day" />.</param>
    /// <param name="provenances">The provenance of each occurrence, index-aligned with <paramref name="overlapping" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="overlapping" /> or <paramref name="provenances" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="provenances" /> does not have the same count as <paramref name="overlapping" />.
    /// </exception>
    public NotableDateCollisionContext(
        DateTime day,
        IReadOnlyList<NotableDate> overlapping,
        IReadOnlyList<NotableDateProvenance> provenances)
    {
        ThrowHelper.ThrowIfNull(overlapping);
        ThrowHelper.ThrowIfNull(provenances);
        if (overlapping.Count != provenances.Count)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_CollisionProvenanceCountMismatch, nameof(provenances));

        Day = day.Date;
        Overlapping = overlapping;
        Provenances = provenances;
    }

    /// <summary>
    /// Gets the calendar day shared by the overlapping occurrences.
    /// </summary>
    /// <returns>The shared day with the time-of-day component truncated.</returns>
    public DateTime Day { get; }

    /// <summary>
    /// Gets the notable dates that apply to <see cref="Day" />. The collection is never empty when supplied by the
    /// service.
    /// </summary>
    /// <returns>The overlapping notable dates.</returns>
    public IReadOnlyList<NotableDate> Overlapping { get; }

    /// <summary>
    /// Gets the provenance of each overlapping occurrence, index-aligned with <see cref="Overlapping" />.
    /// </summary>
    /// <returns>The provenance values.</returns>
    public IReadOnlyList<NotableDateProvenance> Provenances { get; }
}
