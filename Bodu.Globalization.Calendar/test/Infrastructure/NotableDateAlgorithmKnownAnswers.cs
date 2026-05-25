// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmKnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data-source and
/// display-name helpers for every <see cref="INotableDateAlgorithm" /> known-answer test in the calendar test
/// suite. One method per algorithm exposes the full known-answer table; a matching <c>*Smoke</c> method exposes
/// the 1-5 representative rows that stay in the default BVT run.
/// </summary>
/// <remarks>
/// <para>
/// Wire methods up with
/// <c>[DynamicData(nameof(NotableDateAlgorithmKnownAnswers.Losar),
/// typeof(NotableDateAlgorithmKnownAnswers),
/// DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),
/// DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]</c>. MSTest 4.x requires
/// <c>DynamicDataDisplayNameDeclaringType</c> to be set explicitly because the constructor-supplied
/// <see cref="Type" /> argument is used only for the data-source method.
/// </para>
/// <para>
/// Full datasets that exceed the BVT row-count budget (rule of thumb: more than ~50 rows) are routed through a
/// <c>[TestCategory("Regression")]</c> test method so they do not bloat the default build run. The smaller
/// <c>*Smoke</c> providers stay uncategorised and execute on every BVT build.
/// </para>
/// </remarks>
public static class NotableDateAlgorithmKnownAnswers
{
    /// <summary>
    /// Enumerates one <see cref="AlgorithmFactoryCase" /> row per shipped <see cref="INotableDateAlgorithm" />
    /// implementation. Drives the boundary-contract tests in <c>NotableDateAlgorithmContractTests</c> across
    /// every algorithm from a single set of <c>[DataTestMethod]</c> declarations.
    /// </summary>
    /// <returns>A sequence of single-element object arrays whose only entry is an
    /// <see cref="AlgorithmFactoryCase" />.</returns>
    public static IEnumerable<object[]> AllAlgorithmFactories()
    {
        yield return new object[]
        {
            new AlgorithmFactoryCase
            {
                Algorithm = "Losar",
                Factory = () => new LosarNotableDateAlgorithm(),
            },
        };
    }

    /// <summary>
    /// Provides the smoke subset of Losar known-answer rows that runs on every BVT build. One row per
    /// representative year where the algorithm is known to agree with the official Tibetan lunisolar calendar
    /// within a one-day tolerance.
    /// </summary>
    /// <returns>A sequence of single-element object arrays whose only entry is a
    /// <see cref="NotableDateAlgorithmKnownAnswer" />.</returns>
    public static IEnumerable<object[]> LosarSmoke()
    {
        yield return Row(2024, new DateTime(2024, 2, 10));
    }

    /// <summary>
    /// Provides the full Losar known-answer table. Each row identifies a year where the lunar-approximation
    /// algorithm is expected to fall within +/- one day of the official Tibetan New Year date; years where
    /// the Tibetan calendar diverges by an intercalary month are deliberately excluded.
    /// </summary>
    /// <returns>A sequence of single-element object arrays whose only entry is a
    /// <see cref="NotableDateAlgorithmKnownAnswer" />.</returns>
    public static IEnumerable<object[]> Losar()
    {
        yield return Row(2021, new DateTime(2021, 2, 12));
        yield return Row(2024, new DateTime(2024, 2, 10));
    }

    /// <summary>
    /// Renders a <see cref="NotableDateAlgorithmKnownAnswer" /> or <see cref="AlgorithmFactoryCase" /> row as
    /// the MSTest display name. Algorithm-known-answer rows render as
    /// <c>"Losar | 2024 | Default -&gt; 2024-02-10 [+/- 1d]"</c>; factory rows render as just the
    /// algorithm label, for example <c>"Losar"</c>.
    /// </summary>
    /// <param name="methodInfo">The test method under discovery. Unused; required by
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />.</param>
    /// <param name="data">The single-element row produced by one of the provider methods.</param>
    /// <returns>An ASCII display name no longer than ~100 characters.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        _ = methodInfo;

        return data[0] switch
        {
            NotableDateAlgorithmKnownAnswer ka => FormatKnownAnswer(ka),
            AlgorithmFactoryCase fc => fc.Algorithm,
            _ => string.Join(", ", data),
        };
    }

    /// <summary>
    /// Builds a <see cref="NotableDateAlgorithmKnownAnswer" /> row for the Losar algorithm with a one-day
    /// tolerance. Used internally by <see cref="Losar" /> and <see cref="LosarSmoke" /> so each row remains
    /// a single line in the data tables.
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published Tibetan New Year date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] Row(int year, DateTime expectedDate) =>
        new object[]
        {
            new NotableDateAlgorithmKnownAnswer
            {
                Algorithm = "Losar",
                Factory = () => new LosarNotableDateAlgorithm(),
                Year = year,
                ExpectedDate = expectedDate,
                ToleranceDays = 1,
            },
        };

    /// <summary>
    /// Formats a <see cref="NotableDateAlgorithmKnownAnswer" /> row as a pipe-separated, ASCII-only display
    /// name. Suffixes the tolerance window when non-zero and the provenance source when populated.
    /// </summary>
    /// <param name="ka">The row to format.</param>
    /// <returns>The rendered display name.</returns>
    private static string FormatKnownAnswer(NotableDateAlgorithmKnownAnswer ka)
    {
        string calendar = ka.CalendarKind switch
        {
            AlgorithmCalendarKind.Gregorian => "Gregorian",
            AlgorithmCalendarKind.Julian => "Julian",
            _ => "Default",
        };

        string tolerance = ka.ToleranceDays > 0 ? $" [+/- {ka.ToleranceDays}d]" : string.Empty;
        string source = !string.IsNullOrEmpty(ka.Source) ? $" [{ka.Source}]" : string.Empty;

        return $"{ka.Algorithm} | {ka.Year} | {calendar} -> {ka.ExpectedDate:yyyy-MM-dd}{tolerance}{source}";
    }
}
