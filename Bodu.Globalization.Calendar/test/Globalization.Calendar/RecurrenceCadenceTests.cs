// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCadenceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the periodic recurrence cadence (<c>everyYears</c>/<c>anchorYear</c>) on <see cref="RuleApplicability" />,
/// which restores v1's <c>OccurrenceYears</c> modulo behaviour so a rule can recur every N years from an anchor without
/// enumerating each year.
/// </summary>
[TestClass]
public sealed class RecurrenceCadenceTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds an applicability with the supplied cadence and bounds.
    /// </summary>
    /// <param name="fromYear">The lower year bound, or <see langword="null" />.</param>
    /// <param name="everyYears">The recurrence interval, or <see langword="null" />.</param>
    /// <param name="anchorYear">The cadence anchor, or <see langword="null" />.</param>
    /// <returns>The applicability.</returns>
    private static RuleApplicability App(int? fromYear, int? everyYears, int? anchorYear) =>
        new(CalendarSystem.Gregorian, fromYear, null, Array.Empty<string>(), Array.Empty<int>(), Array.Empty<int>(), everyYears, anchorYear);

    /// <summary>
    /// Verifies that a cadence anchored on <c>fromYear</c> (2024, every four years) applies only on the on-cadence
    /// years and never below the lower bound.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expected">Whether the cadence applies in that year.</param>
    [TestMethod]
    [DataRow(2024, true)]
    [DataRow(2025, false)]
    [DataRow(2026, false)]
    [DataRow(2027, false)]
    [DataRow(2028, true)]
    [DataRow(2023, false)]  // below fromYear
    public void AppliesTo_EveryFourYearsFromYear_ShouldApplyOnCadenceOnly(int year, bool expected)
    {
        RuleApplicability a = App(2024, 4, null);

        Assert.AreEqual(expected, a.AppliesTo(Territory, year));
    }

    /// <summary>
    /// Verifies that a cadence without a lower bound anchors on year zero, so an every-four-years cadence applies on
    /// years divisible by four.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expected">Whether the cadence applies in that year.</param>
    [TestMethod]
    [DataRow(2020, true)]
    [DataRow(2021, false)]
    [DataRow(2022, false)]
    [DataRow(2023, false)]
    [DataRow(2024, true)]
    public void AppliesTo_EveryFourYearsWithoutFromYear_ShouldAnchorOnZero(int year, bool expected)
    {
        RuleApplicability a = App(null, 4, null);

        Assert.AreEqual(expected, a.AppliesTo(Territory, year));
    }

    /// <summary>
    /// Verifies that an explicit anchor year (2024) sets the cadence phase, including on-cadence years before the
    /// anchor.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expected">Whether the cadence applies in that year.</param>
    [TestMethod]
    [DataRow(2024, true)]
    [DataRow(2028, true)]
    [DataRow(2020, true)]   // on cadence before the anchor
    [DataRow(2025, false)]
    public void AppliesTo_EveryFourYearsWithExplicitAnchor_ShouldAnchorThere(int year, bool expected)
    {
        RuleApplicability a = App(null, 4, 2024);

        Assert.AreEqual(expected, a.AppliesTo(Territory, year));
    }

    /// <summary>
    /// Verifies that a cadence of one (or zero) imposes no restriction, applying in every year.
    /// </summary>
    /// <param name="everyYears">The recurrence interval under test.</param>
    /// <param name="year">The civil year under test.</param>
    [TestMethod]
    [DataRow(1, 2024)]
    [DataRow(1, 2025)]
    [DataRow(0, 2024)]
    [DataRow(0, 2025)]
    public void AppliesTo_EveryYearsOfOneOrZero_ShouldApplyAnnually(int everyYears, int year)
    {
        RuleApplicability a = App(null, everyYears, null);

        Assert.IsTrue(a.AppliesTo(Territory, year));
    }

    /// <summary>
    /// A quadrennial fixed-date rule authored with <c>everyYears</c>.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.cadence">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="quad-games" displayName="Quad Games" category="Observance">
          <Rules><Rule id="x"><Applicability fromYear="2024" everyYears="4" /><Strategy><Fixed month="July" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that a quadrennial rule resolves end to end through the service, emitting one occurrence on its
    /// on-cadence years and nothing off cadence.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedCount">The expected number of emitted occurrences on 1 July of that year.</param>
    [TestMethod]
    [DataRow(2024, 1)]  // on cadence
    [DataRow(2025, 0)]  // off cadence
    [DataRow(2028, 1)]  // on cadence
    public void Resolve_QuadrennialRule_ShouldEmitOnlyOnCadenceYears(int year, int expectedCount)
    {
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(Xml));

        Assert.HasCount(expectedCount, service.Resolve(new DateOnly(year, 7, 1), Territory));
    }
}
