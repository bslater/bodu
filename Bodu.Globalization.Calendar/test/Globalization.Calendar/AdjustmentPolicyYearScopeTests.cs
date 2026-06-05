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
        var xml = """
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
    /// Verifies that a <c>fromYear</c> scope applies the substitution from that year onward but not before.
    /// </summary>
    [TestMethod]
    public void FromYear_AppliesFromThatYearOnward()
    {
        INotableDateService service = Build("""<Scope fromYear="2022" />""");

        NotableDate applied = ResolveNewYear(service, 2022);
        Assert.IsTrue(applied.IsObserved);
        Assert.AreEqual(new DateOnly(2022, 1, 3), applied.Date);

        // 1 January 2011 is also a Saturday, but it precedes the policy's fromYear, so no substitution applies.
        NotableDate notApplied = ResolveNewYear(service, 2011);
        Assert.IsFalse(notApplied.IsObserved);
        Assert.AreEqual(new DateOnly(2011, 1, 1), notApplied.Date);
    }

    /// <summary>
    /// Verifies that a <c>toYear</c> scope applies the substitution up to that year but not after.
    /// </summary>
    [TestMethod]
    public void ToYear_AppliesUpToThatYear()
    {
        INotableDateService service = Build("""<Scope toYear="2022" />""");

        Assert.IsTrue(ResolveNewYear(service, 2022).IsObserved);

        NotableDate after = ResolveNewYear(service, 2023);
        Assert.IsFalse(after.IsObserved);
        Assert.AreEqual(new DateOnly(2023, 1, 1), after.Date);
    }

    /// <summary>
    /// Verifies that an <c>OnlyYear</c> scope applies the substitution only in the listed year.
    /// </summary>
    [TestMethod]
    public void OnlyYears_AppliesOnlyInListedYears()
    {
        INotableDateService service = Build("""<Scope><OnlyYear value="2022" /></Scope>""");

        Assert.IsTrue(ResolveNewYear(service, 2022).IsObserved);
        Assert.IsFalse(ResolveNewYear(service, 2023).IsObserved);
    }

    /// <summary>
    /// Verifies that an <c>ExceptYear</c> scope suppresses the substitution in the listed year while applying elsewhere.
    /// </summary>
    [TestMethod]
    public void ExceptYears_SuppressesListedYears()
    {
        INotableDateService service = Build("""<Scope><ExceptYear value="2022" /></Scope>""");

        Assert.IsFalse(ResolveNewYear(service, 2022).IsObserved);
        Assert.IsTrue(ResolveNewYear(service, 2023).IsObserved);
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

        Assert.IsTrue(ex.Diagnostics.Any(d => d.Code == "BODU-CAL-YEARS"));
    }
}
