// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Produces fully-populated <see cref="NotableDate" /> instances for a specific notable event, year by year — a self-contained
/// alternative to authoring a <see cref="NotableDateRule" /> in XML or JSON when the calculation and metadata are best expressed
/// directly in code.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="INotableDateProvider" /> differs from <see cref="INotableDateAlgorithm" /> in scope and responsibility:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="INotableDateAlgorithm" /> computes only the <em>anchor date</em> for a year. Surrounding metadata
/// (<see cref="NotableDate.Name" />, <see cref="NotableDate.Category" />, <see cref="NotableDate.Tags" />, territory, duration)
/// is supplied by the <see cref="NotableDateRule" /> that references the algorithm. This is the preferred extensibility seam when
/// the same calculation is reused by multiple rules (Easter Sunday, Easter Monday via offset, Good Friday via offset).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="INotableDateProvider" /> bundles both the calculation and the metadata into a single self-describing component. It
/// returns ready-made <see cref="NotableDate" /> values complete with name, category, tags, calendar binding, and non-working
/// flag. Use it when a notable event has no useful presence in the rule store (it is computed dynamically, sourced from an
/// external service, or unique to a particular host).
/// </description>
/// </item>
/// </list>
/// <para>
/// Implementations should report the supported year range via <see cref="MinSupportedYear" /> and <see cref="MaxSupportedYear" />,
/// validate the optional calendar argument, and return an empty list rather than throwing for years outside their supported range
/// or for calendar combinations they cannot service.
/// </para>
/// <para>
/// The built-in base type <c>EasterSundayNotableDateProviderBase</c> demonstrates a typical implementation: it caches the computed
/// date per year and returns a single <see cref="NotableDate" /> with provider-defined name, category, and tags.
/// </para>
/// </remarks>
public interface INotableDateProvider
{
    /// <summary>
    /// Gets the earliest year for which the provider can produce a <see cref="NotableDate" />.
    /// </summary>
    /// <returns>The inclusive minimum year supported by the provider.</returns>
    int MinSupportedYear { get; }

    /// <summary>
    /// Gets the latest year for which the provider can produce a <see cref="NotableDate" />.
    /// </summary>
    /// <returns>The inclusive maximum year supported by the provider.</returns>
    int MaxSupportedYear { get; }

    /// <summary>
    /// Returns a value indicating whether the specified <paramref name="year" /> is supported by the provider.
    /// </summary>
    /// <param name="year">The year to test.</param>
    /// <returns>
    /// <see langword="true" /> if the provider can resolve dates for the specified <paramref name="year" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    bool SupportsYear(int year);

    /// <summary>
    /// Resolves all notable dates produced by the provider for the specified <paramref name="year" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Most providers emit a single <see cref="NotableDate" /> per year. Providers that produce a family of related occurrences
    /// (for example a multi-day festival exposed as a series of individual <see cref="NotableDate" /> entries rather than a
    /// single span) may return more than one element.
    /// </para>
    /// </remarks>
    /// <param name="year">The year for which notable dates should be resolved.</param>
    /// <param name="calendar">
    /// Optional. A calendar context associated with the resolved dates. Providers may reject unsupported calendar types.
    /// </param>
    /// <returns>A read-only list of resolved notable dates for the specified year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="calendar" /> type is not supported by the provider.
    /// </exception>
    IReadOnlyList<NotableDate> GetDates(int year, SysGlobal.Calendar? calendar = null);
}
