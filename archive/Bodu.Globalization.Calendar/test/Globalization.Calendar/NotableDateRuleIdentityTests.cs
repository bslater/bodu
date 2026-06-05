// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleIdentityTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the equality, hashing, and derivation semantics of <see cref="NotableDateRuleIdentity" />.
/// </summary>
[TestClass]
public sealed class NotableDateRuleIdentityTests
{
    /// <summary>
    /// Verifies that two identities with identical components compare equal.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Equals_WhenAllComponentsMatch_ShouldBeTrue()
    {
        NotableDateRuleIdentity a = new("Easter", "western", "AU", typeof(GregorianCalendar));
        NotableDateRuleIdentity b = new("Easter", "western", "AU", typeof(GregorianCalendar));

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that the canonical name comparison is case-insensitive.
    /// </summary>
    [TestMethod]
    public void Equals_WhenNameDiffersByCaseOnly_ShouldBeTrue()
    {
        NotableDateRuleIdentity a = new("easter", null, null, null);
        NotableDateRuleIdentity b = new("EASTER", null, null, null);

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that the rule-level variant comparison is case-insensitive.
    /// </summary>
    [TestMethod]
    public void Equals_WhenRuleNameDiffersByCaseOnly_ShouldBeTrue()
    {
        NotableDateRuleIdentity a = new("Easter", "Western", null, null);
        NotableDateRuleIdentity b = new("Easter", "western", null, null);

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that a territory is normalized to its canonical form so case differences compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenTerritoryDiffersByCaseOnly_ShouldBeTrue()
    {
        NotableDateRuleIdentity a = new("Labour Day", null, "au-nsw", null);
        NotableDateRuleIdentity b = new("Labour Day", null, "AU-NSW", null);

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that variants sharing a canonical name but differing by rule name are distinct.
    /// </summary>
    [TestMethod]
    public void Equals_WhenRuleNameDiffers_ShouldBeFalse()
    {
        NotableDateRuleIdentity western = new("Easter", "western", null, null);
        NotableDateRuleIdentity orthodox = new("Easter", "orthodox", null, null);

        Assert.AreNotEqual(western, orthodox);
    }

    /// <summary>
    /// Verifies that a country-level territory and one of its subdivisions are distinct identities.
    /// </summary>
    [TestMethod]
    public void Equals_WhenTerritoryParentVersusSubdivision_ShouldBeFalse()
    {
        NotableDateRuleIdentity country = new("Labour Day", null, "AU", null);
        NotableDateRuleIdentity subdivision = new("Labour Day", null, "AU-NSW", null);

        Assert.AreNotEqual(country, subdivision);
    }

    /// <summary>
    /// Verifies that identities differing only by calendar type are distinct.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCalendarTypeDiffers_ShouldBeFalse()
    {
        NotableDateRuleIdentity gregorian = new("Christmas", null, null, typeof(GregorianCalendar));
        NotableDateRuleIdentity julian = new("Christmas", null, null, typeof(JulianCalendar));

        Assert.AreNotEqual(gregorian, julian);
    }

    /// <summary>
    /// Verifies that a null calendar type forms its own bucket and does not match a typed calendar.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCalendarTypeNullVersusTyped_ShouldBeFalse()
    {
        NotableDateRuleIdentity agnostic = new("Christmas", null, null, null);
        NotableDateRuleIdentity gregorian = new("Christmas", null, null, typeof(GregorianCalendar));

        Assert.AreNotEqual(agnostic, gregorian);
    }

    /// <summary>
    /// Verifies that equal identities produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenIdentitiesEqual_ShouldMatch()
    {
        NotableDateRuleIdentity a = new("Easter", "western", "au", typeof(GregorianCalendar));
        NotableDateRuleIdentity b = new("EASTER", "WESTERN", "AU", typeof(GregorianCalendar));

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleIdentity.From(NotableDateRule)" /> captures the rule's identity components.
    /// </summary>
    [TestMethod]
    public void From_WhenGivenRule_ShouldCaptureIdentityComponents()
    {
        NotableDateRule rule = new()
        {
            Name = "Easter",
            RuleName = "orthodox",
            TerritoryCode = "GR",
            CalendarType = typeof(JulianCalendar),
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
        };

        NotableDateRuleIdentity identity = NotableDateRuleIdentity.From(rule);

        Assert.AreEqual(new NotableDateRuleIdentity("Easter", "orthodox", "GR", typeof(JulianCalendar)), identity);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleIdentity.From(NotableDateRule)" /> rejects a null rule.
    /// </summary>
    [TestMethod]
    public void From_WhenRuleIsNull_ShouldThrowExactlyArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleIdentity.From(null!);
        });
    }
}
