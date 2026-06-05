// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionHebrewAliasTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the Hebrew month-alias resolution path of <see cref="FixedDateStrategy" /> through the v2 engine. Aliases
/// whose integer index depends on whether the Hebrew year is a leap year are exercised by a calendar-year sweep across
/// Gregorian 2026 (Hebrew 5786 regular and 5787 leap). Ported from the v1
/// <c>NotableDateRuleResolverTests.HebrewAlias</c> tests, with the v1 "unknown alias returns null" case adapted to the
/// v2 model where an unrecognised month token is a load-time validation error.
/// </summary>
[TestClass]
public sealed class StrategyResolutionHebrewAliasTests
{
    /// <summary>
    /// The service over the Hebrew-alias fixture.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("strategy-hebrew-alias.xml"));

    /// <summary>
    /// Verifies that every supported Hebrew month token resolves to a non-null Gregorian date in a year whose sweep
    /// covers both a regular and a leap Hebrew year, driving every arm of the alias resolution through both its
    /// leap-year branches.
    /// </summary>
    /// <param name="notableDateId">The concept whose Hebrew month token is under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("alias-tishri")]
    [DataRow("alias-heshvan")]
    [DataRow("alias-kislev")]
    [DataRow("alias-tevet")]
    [DataRow("alias-shevat")]
    [DataRow("alias-adar-i")]
    [DataRow("alias-last-adar")]
    [DataRow("alias-nisan")]
    [DataRow("alias-iyar")]
    [DataRow("alias-sivan")]
    [DataRow("alias-tammuz")]
    [DataRow("alias-av")]
    [DataRow("alias-elul")]
    public void Resolve_HebrewAlias_ResolvesWithinRequestedYear(string notableDateId)
    {
        List<NotableDate> matches = s_service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.IsTrue(matches.Count >= 1, $"expected the '{notableDateId}' alias to resolve to a Gregorian date in 2026");
        Assert.IsTrue(matches.All(m => m.Date.Year == 2026), $"every '{notableDateId}' occurrence falls inside 2026");
    }

    /// <summary>
    /// Verifies that a Hebrew fixed-date rule whose month token is not a recognised Hebrew month name or alias fails to
    /// load with a <see cref="NotableDateValidationException" /> carrying an error diagnostic. In v1 an unrecognised
    /// alias resolved to <see langword="null" /> at resolution time; the v2 parser reports it during load.
    /// </summary>
    [TestMethod]
    public void Load_UnknownHebrewAlias_ThrowsValidationException()
    {
        string xml = NotableDateFixtures.ReadText("invalid-unknown-hebrew-alias.xml");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error),
            "expected at least one error diagnostic for the unrecognised Hebrew month token");
    }
}
