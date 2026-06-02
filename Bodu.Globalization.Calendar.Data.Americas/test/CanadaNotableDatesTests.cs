// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CanadaNotableDatesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that Canadian holidays modelled with the <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy
/// resolve to the correct civil dates end to end through the Americas data pack — specifically Victoria Day, the Monday
/// immediately preceding 25 May — across a span of years including the on-the-day case.
/// </summary>
[TestClass]
public sealed class CanadaNotableDatesTests
{
    /// <summary>
    /// Verifies that Victoria Day resolves to the Monday on or before 24 May for each year under test.
    /// </summary>
    /// <param name="knownAnswer">The occurrence row supplied by
    /// <see cref="CanadaTerritoryAnswers.VictoriaDayOccurrences" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(CanadaTerritoryAnswers.VictoriaDayOccurrences),
        typeof(CanadaTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void GetNotableDates_WhenVictoriaDay_ShouldResolveMondayOnOrBefore24May(TerritoryNotableDateKnownAnswer knownAnswer) =>
        TerritoryNotableDateAssertions.AssertExpectedOccurrence(knownAnswer);
}
