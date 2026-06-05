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
    /// Verifies that a cadence anchored on <c>fromYear</c> applies only on the on-cadence years.
    /// </summary>
    [TestMethod]
    public void AppliesTo_EveryFourYearsFromYear_ShouldApplyOnCadenceOnly()
    {
        RuleApplicability a = App(2024, 4, null);

        Assert.IsTrue(a.AppliesTo(Territory, 2024));
        Assert.IsFalse(a.AppliesTo(Territory, 2025));
        Assert.IsFalse(a.AppliesTo(Territory, 2026));
        Assert.IsFalse(a.AppliesTo(Territory, 2027));
        Assert.IsTrue(a.AppliesTo(Territory, 2028));
        Assert.IsFalse(a.AppliesTo(Territory, 2023), "below fromYear");
    }

    /// <summary>
    /// Verifies that a cadence without a lower bound anchors on year zero.
    /// </summary>
    [TestMethod]
    public void AppliesTo_EveryFourYearsWithoutFromYear_ShouldAnchorOnZero()
    {
        RuleApplicability a = App(null, 4, null);

        Assert.IsTrue(a.AppliesTo(Territory, 2020));
        Assert.IsFalse(a.AppliesTo(Territory, 2021));
        Assert.IsFalse(a.AppliesTo(Territory, 2022));
        Assert.IsFalse(a.AppliesTo(Territory, 2023));
        Assert.IsTrue(a.AppliesTo(Territory, 2024));
    }

    /// <summary>
    /// Verifies that an explicit anchor year sets the cadence phase, including years before the anchor.
    /// </summary>
    [TestMethod]
    public void AppliesTo_EveryFourYearsWithExplicitAnchor_ShouldAnchorThere()
    {
        RuleApplicability a = App(null, 4, 2024);

        Assert.IsTrue(a.AppliesTo(Territory, 2024));
        Assert.IsTrue(a.AppliesTo(Territory, 2028));
        Assert.IsTrue(a.AppliesTo(Territory, 2020));
        Assert.IsFalse(a.AppliesTo(Territory, 2025));
    }

    /// <summary>
    /// Verifies that a cadence of one (or zero) imposes no restriction, applying every year.
    /// </summary>
    [DataRow(1)]
    [DataRow(0)]
    [TestMethod]
    public void AppliesTo_EveryYearsOfOneOrZero_ShouldApplyAnnually(int everyYears)
    {
        RuleApplicability a = App(null, everyYears, null);

        Assert.IsTrue(a.AppliesTo(Territory, 2024));
        Assert.IsTrue(a.AppliesTo(Territory, 2025));
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
    /// Verifies that a quadrennial rule resolves end to end through the service only on its on-cadence years.
    /// </summary>
    [TestMethod]
    public void Resolve_QuadrennialRule_ShouldEmitOnlyOnCadenceYears()
    {
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(Xml));

        Assert.AreEqual(1, service.Resolve(new DateOnly(2024, 7, 1), Territory).Count, "2024 on cadence");
        Assert.AreEqual(0, service.Resolve(new DateOnly(2025, 7, 1), Territory).Count, "2025 off cadence");
        Assert.AreEqual(1, service.Resolve(new DateOnly(2028, 7, 1), Territory).Count, "2028 on cadence");
    }
}
