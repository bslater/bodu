// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TehranNowruzKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <c>tehran-nowruz</c> observation-based algorithm — Nowruz from the true vernal-equinox instant at the
/// Tehran standard meridian — against published Solar Hijri new-year dates and against the astronomical
/// <see cref="PersianCalendar" /> as a reference oracle.
/// </summary>
[TestClass]
public sealed class TehranNowruzKnownAnswerTests
{
    /// <summary>
    /// Verifies that the calculator produces the published Nowruz (Farvardin 1) date for anchor years spanning both
    /// sides of the equinox-noon boundary, including the March 20 / March 21 transition years.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The published Nowruz date in ISO format.</param>
    [TestMethod]
    [DataRow(1992, "1992-03-21")]
    [DataRow(1996, "1996-03-20")]
    [DataRow(1997, "1997-03-21")]
    [DataRow(2004, "2004-03-20")]
    [DataRow(2008, "2008-03-20")]
    [DataRow(2016, "2016-03-20")]
    [DataRow(2020, "2020-03-20")]
    [DataRow(2024, "2024-03-20")]
    [DataRow(2025, "2025-03-21")]
    [DataRow(2026, "2026-03-21")]
    public void Nowruz_WhenAnchorYear_ShouldMatchPublishedDate(int year, string expected)
    {
        DateOnly? nowruz = TehranEquinoxCalculator.Nowruz(year);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), nowruz);
    }

    /// <summary>
    /// Verifies that a year outside the supported 1800–2200 window yields no date rather than an unreliable guess.
    /// </summary>
    /// <param name="year">The unsupported Gregorian year.</param>
    [TestMethod]
    [DataRow(1799)]
    [DataRow(2201)]
    public void Nowruz_WhenYearOutsideSupportedRange_ShouldReturnNull(int year)
    {
        Assert.IsNull(TehranEquinoxCalculator.Nowruz(year));
    }

    /// <summary>
    /// Verifies that the <c>tehran-nowruz</c> key is registered as a built-in, so documents referencing it pass
    /// load-time validation without a custom registry.
    /// </summary>
    [TestMethod]
    public void Registry_WhenBuiltInsSeeded_ShouldContainTehranNowruzKey()
    {
        Assert.IsTrue(DefaultNotableDateAlgorithms.Registry.Contains("tehran-nowruz"));
    }

    /// <summary>
    /// Verifies that a document rule referencing the <c>tehran-nowruz</c> key loads, validates, and resolves the
    /// published Nowruz date end-to-end through the service.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRuleUsesTehranNowruzKey_ShouldResolvePublishedDate()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.nowruz">
              <NotableDates>
                <NotableDate id="nowruz" displayName="Nowruz" category="PublicHoliday" defaultNonWorkingDay="true">
                  <Rules><Rule id="ir"><Strategy><Algorithm key="tehran-nowruz" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null);
        NotableDateService service = new(resource);

        var matches = service
            .Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)), "IR")
            .Where(r => r.NotableDateId == "nowruz")
            .ToList();

        Assert.HasCount(1, matches);
        Assert.AreEqual(new DateOnly(2025, 3, 21), matches[0].Date);
    }

    /// <summary>
    /// Verifies that across the entire supported window the calculator agrees with the astronomical
    /// <see cref="PersianCalendar" /> — which implements the same true-equinox / Tehran apparent-noon rule — on the
    /// Gregorian date of Farvardin 1, so the two implementations pin each other year by year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Nowruz_WhenSweptAcrossSupportedRange_ShouldMatchPersianCalendarOracle()
    {
        PersianCalendar oracle = new();

        for (int year = 1800; year <= 2200; year++)
        {
            DateOnly? nowruz = TehranEquinoxCalculator.Nowruz(year);
            DateOnly expected = DateOnly.FromDateTime(oracle.ToDateTime(year - 621, 1, 1, 0, 0, 0, 0));

            Assert.IsNotNull(nowruz, $"{year}: no date produced");
            Assert.AreEqual(expected, nowruz.Value, $"{year}: diverged from PersianCalendar");
        }
    }
}
