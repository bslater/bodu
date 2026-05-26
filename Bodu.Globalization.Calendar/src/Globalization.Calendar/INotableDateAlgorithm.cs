// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Computes the anchor date of a single notable event for a supplied year — the calculation strategy plugged in by a
/// <see cref="NotableDateRule" /> whose <see cref="NotableDateRule.Strategy" /> is
/// <see cref="DateResolutionStrategy.Algorithm" />.
/// </summary>
/// <remarks>
/// <para>
/// Algorithms are the extensibility seam for notable dates that cannot be expressed as a fixed month/day or as the <em>
/// n</em>th weekday of a month — for example Easter Sunday (Computus), Lunar New Year, Vesak, Qingming, or any
/// observance whose date varies by year and depends on a non-Gregorian calendar.
/// </para>
/// <para>
/// During resolution, the <see cref="NotableDateService" /> looks up the implementation associated with the rule's
/// <see cref="NotableDateRule.AlgorithmKey" /> in an <see cref="INotableDateAlgorithmRegistry" />, calls
/// <see cref="GetDate" /> for the requested year, and uses the returned <see cref="DateTime" /> as the anchor of the
/// emitted <see cref="NotableDate" />. All other rule metadata — <see cref="NotableDate.Name" />,
/// <see cref="NotableDate.Category" />, <see cref="NotableDate.TerritoryCode" />, <see cref="NotableDate.Tags" />,
/// duration, and any <see cref="ObservanceAdjustment" /> entries — is layered on top by the service. Algorithms
/// therefore concern themselves only with <em>when</em> the event falls, never with <em>how</em> it is labeled or
/// scoped.
/// </para>
/// <para>
/// Implementations should be deterministic, side-effect-free, and safe to invoke concurrently. The same algorithm
/// instance may be consulted by multiple rules (for example "Easter Sunday", "Easter Monday" via offset, and "Good
/// Friday" via offset all share a single Computus implementation) and by multiple threads when filtered queries bypass
/// the per-year cache.
/// </para>
/// <para>
/// Return <see langword="null" /> for years the algorithm cannot service — for example when a calendar is supplied that
/// the algorithm does not understand, or when the requested year falls outside the calendar's supported range. A
/// <see langword="null" /> result causes the rule to produce no occurrence for that year and is preferable to throwing.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// A minimal algorithm that resolves Australia's Melbourne Cup Day (the first Tuesday of November). In practice an
/// algorithm of this form would be expressed as a <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> rule rather
/// than a custom algorithm, but the same shape applies to genuinely complex computations such as Computus or lunar
/// month resolution:
/// </para>
/// <code>
///<![CDATA[
/// public sealed class MelbourneCupAlgorithm : INotableDateAlgorithm
/// {
///     public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
///     {
///         if (year < 1) throw new ArgumentOutOfRangeException(nameof(year));
///
///         DateTime firstOfNovember = new(year, 11, 1);
///         int offset = ((int)DayOfWeek.Tuesday - (int)firstOfNovember.DayOfWeek + 7) % 7;
///         return firstOfNovember.AddDays(offset);
///     }
/// }
///
/// Register and bind to a NotableDateRule via algorithm key:
/// NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
///     .Register("melbourne-cup", new MelbourneCupAlgorithm());
///
/// NotableDateRule melbourneCup = new NotableDateRule
/// {
///     Name = "Melbourne Cup Day",
///     Strategy = DateResolutionStrategy.Algorithm,
///     Category = NotableDateCategory.Cultural,
///     AlgorithmKey = "melbourne-cup",
///     TerritoryCode = "AU-VIC",
///     IsNonWorkingDay = true,
/// };
///]]>
/// </code>
/// </example>
public interface INotableDateAlgorithm
{
    /// <summary>
    /// Computes the anchor date of the notable event for the specified year and optional calendar system.
    /// </summary>
    /// <param name="year">The target year for the calculation. Must be greater than or equal to 1.</param>
    /// <param name="calendar">
    /// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When
    /// <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
    /// </param>
    /// <returns>
    /// The computed <see cref="DateTime" /> representing the notable event in the given year and calendar system, or
    /// <see langword="null" /> when no occurrence exists for that year (for example, the year is outside the
    /// algorithm's supported range or the supplied calendar produces no candidate date).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="calendar" /> type is not supported by the algorithm.
    /// </exception>
    DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null);
}
