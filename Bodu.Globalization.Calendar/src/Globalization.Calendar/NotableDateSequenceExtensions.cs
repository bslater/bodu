// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSequenceExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides extension methods that transform resolved occurrence sequences, such as expanding observed-only results
/// into a timeline that also contains each adjusted occurrence's originally calculated date.
/// </summary>
/// <remarks>
/// <para>
/// Resolution emits occurrences per the winning adjustment policy's <see cref="EmissionMode" />. Data packs that emit
/// <see cref="EmissionMode.ObservedOnly" /> return a single occurrence per adjusted notable date, anchored on the
/// observed date, with the originally calculated date carried only in <see cref="NotableDate.ActualDate" />. The
/// extensions here reconstruct the fuller <see cref="EmissionMode.ActualAndObserved" /> view from such results without
/// re-running resolution.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // 2021: Christmas Day (Sat 25th) and Boxing Day (Sun 26th) were both observed on the following
/// // working days, so an observed-only pack returns occurrences dated 27 and 28 December.
/// IReadOnlyList<NotableDate> resolved = service.Resolve(
///     new DateRange(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 29)), "AU");
///
/// // Expand to the full timeline: 25 and 26 December reappear as actual occurrences.
/// IReadOnlyList<NotableDate> timeline = resolved.WithActualOccurrences();
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDate" /> <seealso cref="EmissionMode" /> <seealso cref="INotableDateService" />
public static class NotableDateSequenceExtensions
{
    /// <summary>
    /// Returns the occurrences together with a synthesized actual occurrence for every observed occurrence whose
    /// originally calculated date is absent from the sequence.
    /// </summary>
    /// <param name="occurrences">The resolved occurrences to expand.</param>
    /// <returns>
    /// The occurrences plus the synthesized actual occurrences, ordered by date, then notable-date id, then rule id
    /// (ordinal) — the same ordering contract as <see cref="INotableDateService.Resolve(DateRange, string)" />. When no
    /// occurrence requires expansion, the input instance is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="occurrences" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// An occurrence is expanded when <see cref="NotableDate.IsObserved" /> is <see langword="true" /> and
    /// <see cref="NotableDate.ActualDate" /> is non-<see langword="null" /> and differs from
    /// <see cref="NotableDate.Date" />. The synthesized occurrence matches the shape the resolution engine emits for
    /// the actual half of <see cref="EmissionMode.ActualAndObserved" />: it is dated on the calculated date with
    /// <see cref="NotableDate.ActualDate" /> equal to that date, <see cref="NotableDate.IsObserved" /> is
    /// <see langword="false" />, and <see cref="NotableDate.AdjustmentPolicyId" /> and
    /// <see cref="NotableDate.AdjustmentReason" /> are cleared; all other fields, including
    /// <see cref="NotableDate.DisplayName" />, carry over unchanged.
    /// </para>
    /// <para>
    /// Synthesis is skipped when the sequence already contains an occurrence with the same
    /// <see cref="NotableDate.Identity" /> on the calculated date, so the method is idempotent and is a no-op for
    /// results produced under <see cref="EmissionMode.ActualAndObserved" />.
    /// </para>
    /// <para>
    /// A synthesized date can precede the range the sequence was resolved for (an observed occurrence inside the range
    /// may have been calculated before it). Apply <see cref="NotableDateFilter.InDateRange(DateOnly, DateOnly)" /> to
    /// the result when strict range containment is required. The expansion is a pure sequence transform: the source
    /// resource's same-day collision policy is not re-applied to the synthesized occurrences.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<NotableDate> WithActualOccurrences(this IReadOnlyList<NotableDate> occurrences)
    {
        ThrowHelper.ThrowIfNull(occurrences);

        if (occurrences.Count == 0)
            return occurrences;

        // Dedupe on (Identity, Date) rather than record equality: Tags is an IReadOnlyList<string>, so the record's
        // generated equality compares it by reference and value-identical rows would never match.
        var occupied = new HashSet<(NotableDateRuleIdentity Identity, DateOnly Date)>();
        foreach (NotableDate occurrence in occurrences)
            occupied.Add((occurrence.Identity, occurrence.Date));

        List<NotableDate>? synthesized = null;
        foreach (NotableDate occurrence in occurrences)
        {
            if (!occurrence.IsObserved || occurrence.ActualDate is not DateOnly actual || actual == occurrence.Date)
                continue;

            if (!occupied.Add((occurrence.Identity, actual)))
                continue;

            synthesized ??= new List<NotableDate>();
            synthesized.Add(occurrence with
            {
                Date = actual,
                IsObserved = false,
                AdjustmentPolicyId = null,
                AdjustmentReason = null,
            });
        }

        if (synthesized is null)
            return occurrences;

        return occurrences
            .Concat(synthesized)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.NotableDateId, StringComparer.Ordinal)
            .ThenBy(o => o.RuleId, StringComparer.Ordinal)
            .ToArray();
    }
}
