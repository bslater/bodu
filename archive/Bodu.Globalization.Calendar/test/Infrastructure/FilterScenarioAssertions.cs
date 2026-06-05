// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterScenarioAssertions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the shared assertion routine and <c>DynamicDataDisplayName</c> helper for
/// <see cref="FilterScenarioKnownAnswer" /> rows.
/// </summary>
public static class FilterScenarioAssertions
{
    /// <summary>
    /// Constructs the service via <see cref="FilterScenarioKnownAnswer.ServiceFactory" />, applies the filter from
    /// <see cref="FilterScenarioKnownAnswer.FilterFactory" />, and asserts that the returned occurrence name set is
    /// equivalent to <see cref="FilterScenarioKnownAnswer.ExpectedNames" />.
    /// </summary>
    /// <param name="ka">The row to verify.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ka" /> is <see langword="null" />.
    /// </exception>
    public static void AssertResultNamesMatch(FilterScenarioKnownAnswer ka)
    {
        ArgumentNullException.ThrowIfNull(ka);

        NotableDateService service = ka.ServiceFactory();
        NotableDateFilter filter = ka.FilterFactory();

        IReadOnlyList<NotableDate> results = ka.Query is { } query
            ? query(service, filter)
            : service.GetNotableDates(ka.Year, filter);

        var actualNames = results.Select(d => d.Name).ToList();

        CollectionAssert.AreEquivalent(
            ka.ExpectedNames.ToList(),
            actualNames,
            $"Filter scenario '{ka.Scenario}': expected names [{string.Join(", ", ka.ExpectedNames)}], got [{string.Join(", ", actualNames)}].");
    }

    /// <summary>
    /// Renders a <see cref="FilterScenarioKnownAnswer" /> row as the MSTest display name. Rows render as
    /// <c>"CategoryFilter_HolidayOnly | year 2024 | expects 2 names"</c>; suffixes the note when populated.
    /// </summary>
    /// <param name="methodInfo">
    /// The test method under discovery. Unused; required by
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />.
    /// </param>
    /// <param name="data">The single-element row produced by the provider method.</param>
    /// <returns>An ASCII display name no longer than ~100 characters.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        _ = methodInfo;

        if (data[0] is not FilterScenarioKnownAnswer ka)
        {
            return string.Join(", ", data);
        }

        var nameCount = ka.ExpectedNames.Count;
        var suffix = nameCount == 1 ? string.Empty : "s";
        var label = $"{ka.Scenario} | year {ka.Year} | expects {nameCount} name{suffix}";

        if (!string.IsNullOrEmpty(ka.Note))
        {
            label += $" [{ka.Note}]";
        }

        return label;
    }
}
