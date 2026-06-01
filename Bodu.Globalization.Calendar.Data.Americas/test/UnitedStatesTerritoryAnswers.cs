// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedStatesTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// United States rule catalogue shipped in the Americas data pack. Covers cherry-picked rule presence / absence
/// assertions and the US-specific named-holiday occurrences (Independence Day, Thanksgiving).
/// </summary>
public static class UnitedStatesTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> UsProvider =>
        AmericasCalendarData.CreateUnitedStatesProvider;

    /// <summary>
    /// Provides the cherry-pick expectations for the United States flattened rule catalogue.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="RuleCatalogueExpectation" />.
    /// </returns>
    public static IEnumerable<object[]> RuleCatalogueExpectations()
    {
        yield return new object[]
        {
            new RuleCatalogueExpectation
            {
                ProviderFactory = UsProvider,
                Territory = "US",
                Includes =
                [
                    "New Year's Day",
                    "Valentine's Day",
                    "Halloween",
                    "Easter Sunday",
                    "Good Friday",
                    "Christmas Day",
                    "Independence Day",
                    "Thanksgiving",
                ],
                Excludes =
                [
                    "Easter Monday",
                    "Whit Monday",
                    "International Workers' Day",
                    "All Saints' Day",
                ],
            },
        };
    }

    /// <summary>
    /// Provides the United States named-holiday occurrences. Currently covers Independence Day on 4 July 2026; future
    /// expansion (Thanksgiving, MLK Day, etc.) appends additional rows here.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="TerritoryNotableDateKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> NamedHolidayOccurrences()
    {
        yield return new object[]
        {
            new TerritoryNotableDateKnownAnswer
            {
                ProviderFactory = UsProvider,
                Territory = "US",
                Year = 2025,
                Name = "Independence Day",
                ExpectedDate = new DateTime(2025, 7, 4),
                ExpectedDayOfWeek = DayOfWeek.Friday,
                Note = "no weekend substitute",
            },
        };
    }
}
