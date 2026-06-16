// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyYearScopeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that an adjustment policy's scope can be limited by year (<c>fromYear</c>/<c>toYear</c>, <c>OnlyYear</c>,
/// <c>ExceptYear</c>), so a substitution rule that came into force or lapsed in a given year applies only then. The
/// fixture mondayises New Year's Day, which is a Saturday in 2022 and a Sunday in 2023.
/// </summary>
[TestClass]
public sealed class AdjustmentPolicyYearScopeTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a service whose mondayisation policy carries the supplied scope element.
    /// </summary>
    /// <param name="scope">The <c>Scope</c> element to embed in the policy.</param>
    /// <returns>A service over the fixture.</returns>
    private static INotableDateService Build(string scope)
    {
        string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.b1">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="mondayise">
              __SCOPE__
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWorkingDay" skipWeekends="true" />
              <Emission mode="ObservedOnly" reason="Substitute" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="mondayise" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """.Replace("__SCOPE__", scope, StringComparison.Ordinal);

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Resolves the single New Year occurrence in a year's opening week.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="year">The year to resolve.</param>
    /// <returns>The single occurrence.</returns>
    private static NotableDate ResolveNewYear(INotableDateService service, int year) =>
        service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 1, 7)), Territory).Single();

    /// <summary>
    /// Verifies that a <c>fromYear="2022"</c> scope applies the substitution from that year onward but not before,
    /// emitting the observed Monday in scope and the unshifted Saturday actual date out of scope. 1 January is a
    /// Saturday in both 2011 and 2022.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedObserved">Whether the substitution applies in that year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [DataRow(2022, true, 1, 3)]    // in scope -> observed Monday 3 January
    [DataRow(2011, false, 1, 1)]   // before fromYear -> unshifted Saturday 1 January
    public void FromYear_AppliesFromThatYearOnward(int year, bool expectedObserved, int expectedMonth, int expectedDay)
    {
        INotableDateService service = Build("""<Scope fromYear="2022" />""");

        NotableDate occurrence = ResolveNewYear(service, year);

        Assert.AreEqual(
            (expectedObserved, new DateOnly(year, expectedMonth, expectedDay)),
            (occurrence.IsObserved, occurrence.Date));
    }

    /// <summary>
    /// Verifies that a <c>toYear="2022"</c> scope applies the substitution up to that year but not after, emitting the
    /// observed Monday in scope and the unshifted weekend actual date out of scope. 1 January is a Saturday in 2022 and
    /// a Sunday in 2023.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedObserved">Whether the substitution applies in that year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [DataRow(2022, true, 1, 3)]    // in scope -> observed Monday 3 January
    [DataRow(2023, false, 1, 1)]   // after toYear -> unshifted Sunday 1 January
    public void ToYear_AppliesUpToThatYear(int year, bool expectedObserved, int expectedMonth, int expectedDay)
    {
        INotableDateService service = Build("""<Scope toYear="2022" />""");

        NotableDate occurrence = ResolveNewYear(service, year);

        Assert.AreEqual(
            (expectedObserved, new DateOnly(year, expectedMonth, expectedDay)),
            (occurrence.IsObserved, occurrence.Date));
    }

    /// <summary>
    /// Verifies that an <c>OnlyYear="2022"</c> scope applies the substitution only in the listed year.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedObserved">Whether the substitution applies in that year.</param>
    [TestMethod]
    [DataRow(2022, true)]   // listed year -> observed
    [DataRow(2023, false)]  // unlisted year -> not observed
    public void OnlyYears_AppliesOnlyInListedYears(int year, bool expectedObserved)
    {
        INotableDateService service = Build("""<Scope><OnlyYear value="2022" /></Scope>""");

        Assert.AreEqual(expectedObserved, ResolveNewYear(service, year).IsObserved);
    }

    /// <summary>
    /// Verifies that an <c>ExceptYear="2022"</c> scope suppresses the substitution in the listed year while applying
    /// elsewhere.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedObserved">Whether the substitution applies in that year.</param>
    [TestMethod]
    [DataRow(2022, false)]  // excluded year -> not observed
    [DataRow(2023, true)]   // other year -> observed
    public void ExceptYears_SuppressesListedYears(int year, bool expectedObserved)
    {
        INotableDateService service = Build("""<Scope><ExceptYear value="2022" /></Scope>""");

        Assert.AreEqual(expectedObserved, ResolveNewYear(service, year).IsObserved);
    }

    /// <summary>
    /// Verifies that a scope whose <c>fromYear</c> exceeds its <c>toYear</c> fails validation at load.
    /// </summary>
    [TestMethod]
    public void Scope_WhenFromYearAfterToYear_ShouldFailValidation()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = Build("""<Scope fromYear="2030" toYear="2020" />""");
        });

        Assert.Contains(d => d.Code == "BODU-CAL-YEARS", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a scope restricting the policy by territory, calendar, category, notable date, or rule that the
    /// resolution does not satisfy skips the substitution, leaving the occurrence unobserved. 1 January 2022 is a
    /// Saturday, so an in-scope policy would otherwise shift it.
    /// </summary>
    [TestMethod]
    [DataRow("""<Scope><Territory code="ZZ" /></Scope>""", DisplayName = "Territory mismatch")]
    [DataRow("""<Scope><Calendar name="Hebrew" /></Scope>""", DisplayName = "Calendar mismatch")]
    [DataRow("""<Scope><Category value="Observance" /></Scope>""", DisplayName = "Category mismatch")]
    [DataRow("""<Scope><NotableDate ref="other-nd" /></Scope>""", DisplayName = "Notable-date mismatch")]
    [DataRow("""<Scope><Rule notableDateRef="new-year" ruleRef="other-rule" /></Scope>""", DisplayName = "Rule mismatch")]
    public void Scope_WhenFilterDoesNotMatch_ShouldNotApplySubstitution(string scope)
    {
        NotableDate occurrence = ResolveNewYear(Build(scope), 2022);

        Assert.IsFalse(occurrence.IsObserved);
    }
}
