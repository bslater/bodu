// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSequenceExtensionsTests.WithActualOccurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSequenceExtensionsTests
{
    /// <summary>
    /// Verifies that when both adjacent holidays are observed on substitute days (2021: Christmas Saturday, Boxing Day
    /// Sunday), expansion inserts their actual occurrences and the synthesized rows match the shape the engine emits
    /// for <c>ActualAndObserved</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void WithActualOccurrences_WhenBothAdjacentHolidaysObserved_ShouldInsertActualRows()
    {
        IReadOnlyList<NotableDate> resolved = CreateService()
            .Resolve(new DateRange(new DateOnly(2021, 12, 20), new DateOnly(2021, 12, 31)), "AU");

        IReadOnlyList<NotableDate> timeline = resolved.WithActualOccurrences();

        CollectionAssert.AreEqual(
            new[]
            {
                (new DateOnly(2021, 12, 25), "christmas-day", false),
                (new DateOnly(2021, 12, 26), "boxing-day", false),
                (new DateOnly(2021, 12, 27), "christmas-day", true),
                (new DateOnly(2021, 12, 28), "boxing-day", true),
            },
            timeline.Select(o => (o.Date, o.NotableDateId, o.IsObserved)).ToArray());

        NotableDate christmasActual = timeline[0];
        NotableDateAssert.AssertEqual(
            christmasActual,
            date: new DateOnly(2021, 12, 25),
            actualDate: new DateOnly(2021, 12, 25),
            isObserved: false,
            notableDateId: "christmas-day",
            ruleId: "au-fixed-dec-25",
            displayName: "Christmas Day",
            territory: "AU",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: null);
        Assert.IsNull(christmasActual.AdjustmentReason);
        Assert.IsTrue(christmasActual.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that when only one holiday shifted (2016: Christmas observed Tuesday, Boxing Day kept its actual
    /// Monday), expansion inserts only the shifted holiday's actual occurrence and passes the unadjusted occurrence
    /// through as the same instance.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenOnlyObservedRowHasShiftedDate_ShouldInsertOnlyItsActual()
    {
        IReadOnlyList<NotableDate> resolved = CreateService()
            .Resolve(new DateRange(new DateOnly(2016, 12, 20), new DateOnly(2016, 12, 31)), "AU");

        IReadOnlyList<NotableDate> timeline = resolved.WithActualOccurrences();

        CollectionAssert.AreEqual(
            new[]
            {
                (new DateOnly(2016, 12, 25), "christmas-day", false),
                (new DateOnly(2016, 12, 26), "boxing-day", false),
                (new DateOnly(2016, 12, 27), "christmas-day", true),
            },
            timeline.Select(o => (o.Date, o.NotableDateId, o.IsObserved)).ToArray());

        NotableDate boxing = resolved.Single(o => o.NotableDateId == "boxing-day");
        Assert.AreSame(boxing, timeline.Single(o => o.NotableDateId == "boxing-day"));
    }

    /// <summary>
    /// Verifies that a result containing no observed occurrences (2019: both holidays on weekdays) is returned as the
    /// same instance, with no re-sort or reallocation.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenNoRowObserved_ShouldReturnSameInstance()
    {
        IReadOnlyList<NotableDate> resolved = CreateService()
            .Resolve(new DateRange(new DateOnly(2019, 12, 20), new DateOnly(2019, 12, 31)), "AU");

        Assert.HasCount(2, resolved);
        Assert.IsFalse(resolved.Any(o => o.IsObserved));
        Assert.AreSame(resolved, resolved.WithActualOccurrences());
    }

    /// <summary>
    /// Verifies that applying the expansion twice yields the same sequence as applying it once, because the first pass
    /// materializes exactly the occurrences the dedupe key checks for.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenAppliedTwice_ShouldBeIdempotent()
    {
        IReadOnlyList<NotableDate> resolved = CreateService()
            .Resolve(new DateRange(new DateOnly(2021, 12, 20), new DateOnly(2021, 12, 31)), "AU");

        IReadOnlyList<NotableDate> once = resolved.WithActualOccurrences();
        IReadOnlyList<NotableDate> twice = once.WithActualOccurrences();

        Assert.AreSame(once, twice);
    }

    /// <summary>
    /// Verifies that when the sequence already contains the actual occurrence (an <c>ActualAndObserved</c> emission),
    /// no duplicate is synthesized and the input instance is returned unchanged.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenActualRowAlreadyPresent_ShouldNotDuplicate()
    {
        var actual = Create(new DateOnly(2021, 12, 25), actualDate: new DateOnly(2021, 12, 25));
        var observed = Create(
            new DateOnly(2021, 12, 27),
            actualDate: new DateOnly(2021, 12, 25),
            isObserved: true,
            adjustmentPolicyId: "weekend-substitute",
            adjustmentReason: "Substitute public holiday");
        NotableDate[] input = [actual, observed];

        Assert.AreSame(input, input.WithActualOccurrences());
    }

    /// <summary>
    /// Verifies that an observed occurrence carrying no calculated date (possible from provider-contributed
    /// occurrences) is skipped rather than treated as an error.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenActualDateIsNull_ShouldSkipSynthesis()
    {
        NotableDate[] input = [Create(new DateOnly(2021, 12, 27), actualDate: null, isObserved: true)];

        Assert.AreSame(input, input.WithActualOccurrences());
    }

    /// <summary>
    /// Verifies that an observed occurrence whose calculated date equals its emitted date (a declined custom
    /// adjustment) synthesizes nothing.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenActualDateEqualsDate_ShouldSkipSynthesis()
    {
        NotableDate[] input =
            [Create(new DateOnly(2021, 12, 25), actualDate: new DateOnly(2021, 12, 25), isObserved: true)];

        Assert.AreSame(input, input.WithActualOccurrences());
    }

    /// <summary>
    /// Verifies that an empty sequence is returned as the same instance.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenEmpty_ShouldReturnEmpty()
    {
        IReadOnlyList<NotableDate> input = Array.Empty<NotableDate>();

        Assert.AreSame(input, input.WithActualOccurrences());
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IReadOnlyList<NotableDate>)null!).WithActualOccurrences();
        });

        Assert.AreEqual("occurrences", ex.ParamName);
    }

    /// <summary>
    /// Verifies that synthesized occurrences landing on a shared date are ordered by notable-date id then rule id using
    /// ordinal comparison, matching the <see cref="INotableDateService.Resolve(DateRange, string)" /> ordering
    /// contract.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenSynthesizedRowsShareDate_ShouldOrderByIdThenRuleIdOrdinal()
    {
        var shared = new DateOnly(2021, 1, 4);
        NotableDate[] input =
        [
            Create(new DateOnly(2021, 1, 6), actualDate: shared, isObserved: true, notableDateId: "a-day"),
            Create(new DateOnly(2021, 1, 7), actualDate: shared, isObserved: true, notableDateId: "B-day"),
        ];

        IReadOnlyList<NotableDate> timeline = input.WithActualOccurrences();

        CollectionAssert.AreEqual(
            new[]
            {
                (shared, "B-day"),
                (shared, "a-day"),
                (new DateOnly(2021, 1, 6), "a-day"),
                (new DateOnly(2021, 1, 7), "B-day"),
            },
            timeline.Select(o => (o.Date, o.NotableDateId)).ToArray());
    }

    /// <summary>
    /// Verifies that a multi-day observed occurrence synthesizes an actual occurrence with the same duration, so
    /// <see cref="NotableDate.EndDate" /> recomputes from the calculated date.
    /// </summary>
    [TestMethod]
    public void WithActualOccurrences_WhenObservedRowSpansMultipleDays_ShouldPreserveDurationOnSynthesizedRow()
    {
        NotableDate[] input =
        [
            Create(new DateOnly(2021, 1, 8), actualDate: new DateOnly(2021, 1, 6), isObserved: true, durationDays: 2),
        ];

        IReadOnlyList<NotableDate> timeline = input.WithActualOccurrences();

        Assert.HasCount(2, timeline);
        NotableDate synthesized = timeline[0];
        Assert.AreEqual(new DateOnly(2021, 1, 6), synthesized.Date);
        Assert.AreEqual(2, synthesized.DurationDays);
        Assert.AreEqual(new DateOnly(2021, 1, 7), synthesized.EndDate);
    }
}
