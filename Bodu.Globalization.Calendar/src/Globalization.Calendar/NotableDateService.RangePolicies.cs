// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateService.RangePolicies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Implements the resolution path for a resource whose span collision policy settles overlapping spans or whose
/// observed-date range policy includes an occurrence by its actual span.
/// </summary>
public sealed partial class NotableDateService
{
    /// <summary>The number of civil years a query widens what it gathers, beyond its one-year margin on each side, while an overlap chain that reaches the window crosses the gathered boundary.</summary>
    private const int MaxChainWideningYears = 10;

    /// <summary>
    /// Determines whether the resource needs the range-policy resolution path: a span collision policy other than
    /// <see cref="CollisionPolicy.KeepAll" />, or an observed-date range policy other than
    /// <see cref="ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when either policy is not its default; otherwise <see langword="false" />.
    /// </returns>
    private bool UsesRangePolicies() =>
        _resource.ResolutionPolicy.SpanCollisionPolicy != CollisionPolicy.KeepAll
        || _resource.ResolutionPolicy.ObservedDateRangePolicy != ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion;

    /// <summary>
    /// Resolves a window for a resource whose span collision policy or observed-date range policy is not its default:
    /// emits every occurrence of the gathered civil years, settles the same-day groups and then the overlap chains that
    /// can reach the result, and keeps the survivors the observed-date range policy includes in the window.
    /// </summary>
    /// <param name="range">The requested window.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The included survivors, ordered by emitted date.</returns>
    /// <remarks>
    /// <para>
    /// The years gathered start one civil year either side of the window. While the span policy settles chains and a
    /// chain that holds an included occurrence can reach an occurrence of an ungathered year, the gathered years widen
    /// one year at a time on that side, up to <see cref="MaxChainWideningYears" /> years beyond the one-year margin. A
    /// chain still open at that limit, one that never ends, is settled over what was gathered.
    /// </para>
    /// <para>
    /// A same-day group or a chain that holds no occurrence the window can include is left unsettled, so a custom
    /// resolver is consulted only for groups that can contribute to the result.
    /// </para>
    /// </remarks>
    private List<NotableDate> ResolveWithRangePolicies(DateRange range, string territory)
    {
        ResolutionPolicy policy = _resource.ResolutionPolicy;
        bool settlesSpans = policy.SpanCollisionPolicy != CollisionPolicy.KeepAll;
        bool higherWins = policy.PriorityDirection == PriorityDirection.HigherWins;

        int firstYear = Math.Max(1, range.StartDate.Year - 1);
        int lastYear = Math.Min(9999, range.EndDate.Year + 1);
        int earliestYear = Math.Max(1, firstYear - MaxChainWideningYears);
        int latestYear = Math.Min(9999, lastYear + MaxChainWideningYears);

        while (true)
        {
            List<NotableDate> occurrences = EmitGatheredYears(territory, firstYear, lastYear);
            List<(int Start, int End)> chains = FindOverlapChains(occurrences);

            if (settlesSpans)
            {
                (bool openStart, bool openEnd) = FindOpenChains(occurrences, chains, range, firstYear, lastYear);
                bool widenStart = openStart && firstYear > earliestYear;
                bool widenEnd = openEnd && lastYear < latestYear;
                if (widenStart || widenEnd)
                {
                    firstYear -= widenStart ? 1 : 0;
                    lastYear += widenEnd ? 1 : 0;
                    continue;
                }
            }

            // A same-day group can reach the result through its own included occurrences or, when the span policy
            // settles chains, through the chain it belongs to.
            bool[] relevantDay = new bool[occurrences.Count];
            foreach ((int start, int end) in chains)
            {
                bool chainReaches = false;
                for (int i = start; i < end && !chainReaches; i++)
                    chainReaches = IsIncluded(occurrences[i], range);

                for (int i = start; i < end; i++)
                    relevantDay[i] = settlesSpans ? chainReaches : IsIncluded(occurrences[i], range);
            }

            List<NotableDate> settled = SettleSameDayGroups(occurrences, relevantDay, policy.SameDayCollisionPolicy, higherWins);

            if (settlesSpans)
                settled = SettleOverlapChains(settled, range, policy.SpanCollisionPolicy, higherWins);

            List<NotableDate> included = new(settled.Count);
            foreach (NotableDate occurrence in settled)
            {
                if (IsIncluded(occurrence, range))
                    included.Add(occurrence);
            }

            // A custom resolver chooses the order of what it keeps within its group; a stable sort by date keeps that
            // order while restoring date order across the groups.
            return [.. included.OrderBy(o => o.Date)];
        }
    }

    /// <summary>
    /// Emits every occurrence of the gathered civil years, the providers' included, ordered by emitted date, then
    /// identity.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="firstYear">The first civil year gathered.</param>
    /// <param name="lastYear">The last civil year gathered.</param>
    /// <returns>The ordered occurrences.</returns>
    private List<NotableDate> EmitGatheredYears(string territory, int firstYear, int lastYear)
    {
        StrategyResolutionContext context = new(_resource, _algorithms, territory);
        HashSet<DateOnly> occupied = new();
        List<ResolutionCandidate> candidates = GatherCandidates(context, territory, firstYear, lastYear, occupied);

        candidates.Sort(CompareForPlacement);

        DateRange everything = new(DateOnly.MinValue, DateOnly.MaxValue);
        List<NotableDate> results = new();
        foreach (ResolutionCandidate candidate in candidates)
            EmitCandidate(results, candidate, territory, occupied, everything, context);

        if (_providers is not null)
        {
            DateRange gathered = new(new DateOnly(firstYear, 1, 1), new DateOnly(lastYear, 12, 31));
            foreach (INotableDateProvider provider in _providers)
            {
                foreach (NotableDate occurrence in provider.GetNotableDates(gathered, territory))
                {
                    if (occurrence is not null)
                        results.Add(occurrence);
                }
            }
        }

        return [.. results
            .OrderBy(r => r.Date)
            .ThenBy(r => r.NotableDateId, StringComparer.Ordinal)
            .ThenBy(r => r.RuleId, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Partitions date-ordered occurrences into overlap chains: maximal runs in which each occurrence's emitted span
    /// shares a day with the span of the run before it.
    /// </summary>
    /// <param name="ordered">The occurrences, ordered by emitted date.</param>
    /// <returns>Each chain as the start index and the exclusive end index of its run.</returns>
    private static List<(int Start, int End)> FindOverlapChains(List<NotableDate> ordered)
    {
        List<(int Start, int End)> chains = new();
        int start = 0;
        long reach = long.MinValue;

        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > start && ordered[i].Date.DayNumber > reach)
            {
                chains.Add((start, i));
                start = i;
                reach = long.MinValue;
            }

            reach = Math.Max(reach, LastDayNumber(ordered[i]));
        }

        if (ordered.Count > 0)
            chains.Add((start, ordered.Count));

        return chains;
    }

    /// <summary>
    /// Determines whether a chain that holds an included occurrence can reach an occurrence of an ungathered year:
    /// before the gathered years, when it starts within the longest span of their first day, or after them, when it
    /// runs past their last day.
    /// </summary>
    /// <param name="ordered">The occurrences, ordered by emitted date.</param>
    /// <param name="chains">The overlap chains of <paramref name="ordered" />.</param>
    /// <param name="range">The requested window.</param>
    /// <param name="firstYear">The first civil year gathered.</param>
    /// <param name="lastYear">The last civil year gathered.</param>
    /// <returns>Whether such a chain is open at the start and whether one is open at the end.</returns>
    private (bool Start, bool End) FindOpenChains(List<NotableDate> ordered, List<(int Start, int End)> chains, DateRange range, int firstYear, int lastYear)
    {
        int longest = 1;
        foreach (NotableDate occurrence in ordered)
            longest = Math.Max(longest, occurrence.DurationDays);

        // An occurrence of an earlier year ends no later than the longest span after the gathered years begin; one of a
        // later year starts after they end.
        long startBoundary = (long)new DateOnly(firstYear, 1, 1).DayNumber + longest - 1;
        long endBoundary = new DateOnly(lastYear, 12, 31).DayNumber;

        bool openStart = false;
        bool openEnd = false;
        foreach ((int start, int end) in chains)
        {
            bool reaches = false;
            long last = long.MinValue;
            for (int i = start; i < end; i++)
            {
                reaches |= IsIncluded(ordered[i], range);
                last = Math.Max(last, LastDayNumber(ordered[i]));
            }

            if (!reaches)
                continue;

            openStart |= ordered[start].Date.DayNumber < startBoundary;
            openEnd |= last > endBoundary;
        }

        return (openStart, openEnd);
    }

    /// <summary>
    /// Settles the same-day groups whose occurrences can reach the result with the same-day collision policy, keeping
    /// every other occurrence as it is.
    /// </summary>
    /// <param name="ordered">The occurrences, ordered by emitted date.</param>
    /// <param name="relevant">For each occurrence, whether it can reach the result, alone or through its chain.</param>
    /// <param name="policy">The same-day collision policy.</param>
    /// <param name="higherWins">Whether a higher priority value wins.</param>
    /// <returns>The survivors, in order.</returns>
    private List<NotableDate> SettleSameDayGroups(List<NotableDate> ordered, bool[] relevant, CollisionPolicy policy, bool higherWins)
    {
        if (policy == CollisionPolicy.KeepAll)
            return ordered;

        List<NotableDate> kept = new(ordered.Count);
        int index = 0;
        while (index < ordered.Count)
        {
            DateOnly date = ordered[index].Date;
            int end = index;
            bool reaches = false;
            while (end < ordered.Count && ordered[end].Date == date)
            {
                reaches |= relevant[end];
                end++;
            }

            if (end - index > 1 && reaches)
                kept.AddRange(ResolveCollision(date, ordered.GetRange(index, end - index), policy, higherWins));
            else
                kept.AddRange(ordered.GetRange(index, end - index));

            index = end;
        }

        return kept;
    }

    /// <summary>
    /// Settles, with the span collision policy, every overlap chain of two or more occurrences that holds a multi-day
    /// occurrence and an occurrence the window includes, keeping every other occurrence as it is.
    /// </summary>
    /// <param name="settled">The same-day survivors, ordered by emitted date.</param>
    /// <param name="range">The requested window.</param>
    /// <param name="policy">The span collision policy.</param>
    /// <param name="higherWins">Whether a higher priority value wins.</param>
    /// <returns>The survivors, in order within each chain.</returns>
    private List<NotableDate> SettleOverlapChains(List<NotableDate> settled, DateRange range, CollisionPolicy policy, bool higherWins)
    {
        List<NotableDate> kept = new(settled.Count);
        foreach ((int start, int end) in FindOverlapChains(settled))
        {
            List<NotableDate> chain = settled.GetRange(start, end - start);
            bool settles = chain.Count > 1
                && chain.Exists(o => o.DurationDays > 1)
                && chain.Exists(o => IsIncluded(o, range));

            // The chain's group is passed with its earliest emitted date, the date a custom resolver receives.
            kept.AddRange(settles ? ResolveCollision(chain[0].Date, chain, policy, higherWins) : chain);
        }

        return kept;
    }

    /// <summary>
    /// Determines whether the resource's observed-date range policy includes an occurrence in a window: by its emitted
    /// span, by its actual span, from its actual date through its duration, or by either.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="range">The window.</param>
    /// <returns>
    /// <see langword="true" /> when the policy includes the occurrence; otherwise <see langword="false" />.
    /// </returns>
    private bool IsIncluded(NotableDate occurrence, DateRange range) =>
        _resource.ResolutionPolicy.ObservedDateRangePolicy switch
        {
            ObservedDateRangePolicy.ActualOccurrenceControlsInclusion => ActualSpanIntersects(occurrence, range),
            ObservedDateRangePolicy.BothOccurrencesControlInclusion => ActualSpanIntersects(occurrence, range) || SpanIntersects(occurrence, range),
            _ => SpanIntersects(occurrence, range),
        };

    /// <summary>
    /// Determines whether an occurrence's actual span, from its actual date, or its emitted date when it has none,
    /// through its duration, intersects a window.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="range">The inclusive range.</param>
    /// <returns>
    /// <see langword="true" /> when the actual span intersects the range; otherwise <see langword="false" />.
    /// </returns>
    private static bool ActualSpanIntersects(NotableDate occurrence, DateRange range)
    {
        DateOnly start = occurrence.ActualDate ?? occurrence.Date;
        return start <= range.EndDate && (long)start.DayNumber + Math.Max(1, occurrence.DurationDays) - 1 >= range.StartDate.DayNumber;
    }

    /// <summary>
    /// Returns the day number of the last day of an occurrence's emitted span.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <returns>The last day's day number.</returns>
    private static long LastDayNumber(NotableDate occurrence) =>
        (long)occurrence.Date.DayNumber + Math.Max(1, occurrence.DurationDays) - 1;
}
