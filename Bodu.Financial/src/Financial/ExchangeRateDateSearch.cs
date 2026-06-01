// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateDateSearch.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Provides the shared candidate-selection algorithm used to resolve a requested date to an observation index
/// under an <see cref="ExchangeRateDateResolution" /> policy.
/// </summary>
/// <remarks>
/// <para>
/// Both the immutable storage backing <see cref="ExchangeRateSeries" /> and any mutable buffer can route through
/// this helper so that the previous/next/nearest selection rules — including the tie-break semantics encoded in
/// <see cref="ExchangeRateDateResolution.NearestPreferPrevious" /> and
/// <see cref="ExchangeRateDateResolution.NearestPreferNext" /> — have a single source of truth. The helper
/// operates on a <see cref="ReadOnlySpan{T}" /> of day numbers so callers can pass either a full backing array
/// or the active prefix of a growing buffer without allocation.
/// </para>
/// </remarks>
internal static class ExchangeRateDateSearch
{
    /// <summary>
    /// Selects the observation index for the supplied <paramref name="resolution" /> given the previous and next
    /// bracket indices produced by a binary search.
    /// </summary>
    /// <param name="dayNumbers">The sorted, unique day numbers of the underlying observations.</param>
    /// <param name="requestedDayNumber">The day number the caller originally requested.</param>
    /// <param name="resolution">The date-resolution policy in effect.</param>
    /// <param name="previous">The index immediately before the insertion point, or <c>-1</c> if none.</param>
    /// <param name="next">The insertion point, or <c><paramref name="dayNumbers" />.Length</c> if past the end.</param>
    /// <param name="candidate">
    /// When this method returns <see langword="true" />, the index of the selected observation; otherwise
    /// <c>-1</c>.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a candidate was selected; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For <see cref="ExchangeRateDateResolution.Exact" /> the caller should perform the exact-hit check itself
    /// before calling; this helper returns <see langword="false" /> for that value because no fallback
    /// candidate is permitted.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySelectCandidate(
        ReadOnlySpan<int> dayNumbers,
        int requestedDayNumber,
        ExchangeRateDateResolution resolution,
        int previous,
        int next,
        out int candidate)
    {
        var hasPrevious = previous >= 0;
        var hasNext = next < dayNumbers.Length;

        switch (resolution)
        {
            case ExchangeRateDateResolution.PreviousOnOrBefore:
                if (hasPrevious)
                {
                    candidate = previous;
                    return true;
                }
                break;

            case ExchangeRateDateResolution.NextOnOrAfter:
                if (hasNext)
                {
                    candidate = next;
                    return true;
                }
                break;

            case ExchangeRateDateResolution.Nearest:
            case ExchangeRateDateResolution.NearestPreferPrevious:
            case ExchangeRateDateResolution.NearestPreferNext:
                return TrySelectNearest(dayNumbers, requestedDayNumber, resolution, hasPrevious, hasNext, previous, next, out candidate);
        }

        candidate = -1;
        return false;
    }

    /// <summary>
    /// Selects the nearest candidate index, honouring the tie-break preference encoded in
    /// <paramref name="resolution" />.
    /// </summary>
    /// <param name="dayNumbers">The sorted day numbers of the underlying observations.</param>
    /// <param name="requestedDayNumber">The day number the caller originally requested.</param>
    /// <param name="resolution">A <c>Nearest*</c> resolution value.</param>
    /// <param name="hasPrevious">Whether a previous candidate is available.</param>
    /// <param name="hasNext">Whether a next candidate is available.</param>
    /// <param name="previous">The previous candidate index.</param>
    /// <param name="next">The next candidate index.</param>
    /// <param name="candidate">When this method returns <see langword="true" />, the chosen index.</param>
    /// <returns><see langword="true" /> if a candidate was selected; otherwise <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySelectNearest(
        ReadOnlySpan<int> dayNumbers,
        int requestedDayNumber,
        ExchangeRateDateResolution resolution,
        bool hasPrevious,
        bool hasNext,
        int previous,
        int next,
        out int candidate)
    {
        if (!hasPrevious && !hasNext)
        {
            candidate = -1;
            return false;
        }

        if (!hasPrevious)
        {
            candidate = next;
            return true;
        }

        if (!hasNext)
        {
            candidate = previous;
            return true;
        }

        var previousDistance = requestedDayNumber - dayNumbers[previous];
        var nextDistance = dayNumbers[next] - requestedDayNumber;

        if (previousDistance < nextDistance)
        {
            candidate = previous;
            return true;
        }

        if (nextDistance < previousDistance)
        {
            candidate = next;
            return true;
        }

        switch (resolution)
        {
            case ExchangeRateDateResolution.NearestPreferPrevious:
                candidate = previous;
                return true;

            case ExchangeRateDateResolution.NearestPreferNext:
                candidate = next;
                return true;

            default:
                candidate = -1;
                return false;
        }
    }
}
