// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Build()" /> produces a resource whose identity, definition
    /// count, and resolution policy reflect the authored document.
    /// </summary>
    [TestMethod]
    public void Build_WhenDocumentAuthored_ShouldProduceResourceWithAuthoredShape()
    {
        NotableDateResource resource = SampleDocument().Build();

        Assert.AreEqual(
            ("demo.sample", 4, RangeResolution.DuplicatePolicy.KeepFirst, 1),
            (resource.ResourceId, resource.NotableDates.Count, resource.ResolutionPolicy.DuplicatePolicy, resource.AdjustmentPolicies.Count));
    }

    /// <summary>
    /// Verifies that a fixed-date rule built by the builder resolves to its authored month and day.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedStrategy_ShouldResolveAuthoredDate()
    {
        NotableDateService service = ServiceFor(SampleDocument());

        NotableDate newYear = service.Resolve(2026, "US").Single(n => n.NotableDateId == "new-years-day");

        Assert.AreEqual(
            (new DateOnly(2026, 1, 1), "New Year's Day"),
            (newYear.Date, newYear.DisplayName));
    }

    /// <summary>
    /// Verifies that a day-of-week-in-month rule resolves to the correct nth weekday (US Thanksgiving 2026).
    /// </summary>
    [TestMethod]
    public void Build_WhenDayOfWeekInMonthStrategy_ShouldResolveNthWeekday()
    {
        NotableDateService service = ServiceFor(SampleDocument());

        NotableDate thanksgiving = service.Resolve(2026, "US").Single(n => n.NotableDateId == "thanksgiving");

        Assert.AreEqual(new DateOnly(2026, 11, 26), thanksgiving.Date);
    }

    /// <summary>
    /// Verifies that an algorithm-backed rule resolves through the bundled calculator (Western Easter 2026).
    /// </summary>
    [TestMethod]
    public void Build_WhenAlgorithmStrategy_ShouldResolveComputedDate()
    {
        NotableDateService service = ServiceFor(SampleDocument());

        NotableDate easter = service.Resolve(2026, "US").Single(n => n.NotableDateId == "easter-sunday");

        Assert.AreEqual(new DateOnly(2026, 4, 5), easter.Date);
    }

    /// <summary>
    /// Verifies that an offset-from-rule rule resolves relative to the referenced rule (Good Friday = Easter − 2).
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetFromRuleStrategy_ShouldResolveRelativeToReference()
    {
        NotableDateService service = ServiceFor(SampleDocument());

        NotableDate goodFriday = service.Resolve(2026, "US").Single(n => n.NotableDateId == "good-friday");

        Assert.AreEqual(new DateOnly(2026, 4, 3), goodFriday.Date);
    }

    /// <summary>
    /// Verifies that a referenced adjustment policy shifts a weekend occurrence to the next working day and flags it as
    /// observed.
    /// </summary>
    [TestMethod]
    public void Build_WhenAdjustmentPolicyReferenced_ShouldObserveWeekendHolidayOnNextWorkingDay()
    {
        NotableDateService service = ServiceFor(SampleDocument());

        // 1 January 2028 falls on a Saturday; the weekend-to-monday policy moves it to Monday 3 January.
        NotableDate newYear = service.Resolve(2028, "US").Single(n => n.NotableDateId == "new-years-day");

        Assert.AreEqual(
            (new DateOnly(2028, 1, 3), (DateOnly?)new DateOnly(2028, 1, 1), true),
            (newYear.Date, newYear.ActualDate, newYear.IsObserved));
    }

    /// <summary>
    /// Verifies that building a document without a resource id throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenResourceIdMissing_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddNotableDate("x", "X", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Fixed(1, 1)));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    /// <summary>
    /// Verifies that building a definition that has no rules throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenDefinitionHasNoRules_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.x")
            .AddNotableDate("x", "X", NotableDateCategory.Observance, d => { });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }

    /// <summary>
    /// Verifies that building a rule that has no resolution strategy throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenRuleHasNoStrategy_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.x")
            .AddNotableDate("x", "X", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.WithPriority(1)));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
    }
}
