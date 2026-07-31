// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceAsyncExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides asynchronous streaming projections over <see cref="INotableDateService" />, yielding occurrences
/// incrementally for large multi-year range queries.
/// </summary>
/// <remarks>
/// <para>
/// Notable-date resolution is synchronous, CPU-bound work — there is no I/O to overlap — so these projections do not
/// offload to the thread pool. Instead the requested range is resolved one civil year at a time, occurrences are
/// yielded as each year completes, and the iterator yields control cooperatively between years so a consumer awaiting
/// the sequence observes progress (and cancellation) at year granularity rather than only after the whole range has
/// resolved.
/// </para>
/// <para>
/// The streamed sequence is element-for-element identical to the corresponding synchronous
/// <see cref="INotableDateService.Resolve(DateRange, string)" /> call over the same range: the same occurrences, in the
/// same date-then-identity order, with a multi-day occurrence spanning a year boundary yielded exactly once.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateRange decade = new(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));
///
/// await foreach (NotableDate occurrence in service.ResolveAsync(decade, "US", cancellationToken: token))
/// {
///     Console.WriteLine($"{occurrence.Date:yyyy-MM-dd} {occurrence.DisplayName}");
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" /> <seealso cref="NotableDateServiceExtensions" />
/// <seealso href="../guides/calendar/notable-dates.html">Using NotableDateService (guide)</seealso>
public static class NotableDateServiceAsyncExtensions
{
    /// <summary>
    /// Resolves the notable-date occurrences emitted in a date range for the requested territory as an asynchronous
    /// stream, optionally restricted to occurrences matching a filter.
    /// </summary>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="range">The inclusive date range to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">The filter that emitted occurrences must satisfy, or <see langword="null" /> for all.</param>
    /// <param name="cancellationToken">The token observed between resolved years to cancel the stream.</param>
    /// <returns>
    /// An asynchronous sequence of the occurrences emitted in the range, ordered by date then identity — the same
    /// occurrences the synchronous range overloads return.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="range" /> has a start date later than its end date.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> (or a token supplied via
    /// <c>WithCancellation</c>) is cancelled while the stream is being enumerated.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Arguments are validated eagerly, when the method is called; resolution work begins on the first
    /// <c>MoveNextAsync</c>. Cancellation is observed between civil years, so a request over a single year cancels only
    /// before or after that year resolves.
    /// </para>
    /// </remarks>
    public static IAsyncEnumerable<NotableDate> ResolveAsync(
        this INotableDateService service,
        DateRange range,
        string territory,
        NotableDateFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfGreaterThan(range.StartDate, range.EndDate);

        return ResolveAsyncCore(service, range, territory, filter, cancellationToken);
    }

    /// <summary>
    /// Streams the occurrences of a validated range one civil year at a time.
    /// </summary>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="range">The validated inclusive date range.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">The optional occurrence filter.</param>
    /// <param name="cancellationToken">The token observed between resolved years.</param>
    /// <returns>The asynchronous occurrence sequence.</returns>
    private static async IAsyncEnumerable<NotableDate> ResolveAsyncCore(
        INotableDateService service,
        DateRange range,
        string territory,
        NotableDateFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int year = range.StartDate.Year; year <= range.EndDate.Year; year++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool firstChunk = year == range.StartDate.Year;
            DateOnly chunkStart = firstChunk ? range.StartDate : new DateOnly(year, 1, 1);
            DateOnly chunkEnd = year == range.EndDate.Year ? range.EndDate : new DateOnly(year, 12, 31);
            DateRange chunk = new(chunkStart, chunkEnd);

            IReadOnlyList<NotableDate> occurrences = filter is null
                ? service.Resolve(chunk, territory)
                : service.Resolve(chunk, territory, filter);

            foreach (NotableDate occurrence in occurrences)
            {
                // A multi-day span crossing a chunk boundary is included by both adjacent chunk queries. The earlier
                // chunk already yielded it, so later chunks keep only occurrences whose emitted date falls inside the
                // chunk; the first chunk keeps leading spans, matching the single-call inclusion contract.
                if (!firstChunk && occurrence.Date < chunkStart)
                    continue;

                yield return occurrence;
            }

            // Yield control between years so long multi-year streams stay cooperative without thread-pool offloading.
            if (year < range.EndDate.Year)
                await Task.Yield();
        }
    }
}
