// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionEngineTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateResolutionEngine" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionEngineTests
{
    /// <summary>
    /// Verifies that construction rejects a null occurrence resolver.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOccurrenceResolverIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateResolutionEngine(null!);
        });

        Assert.AreEqual("occurrenceResolver", ex.ParamName);
    }

    /// <summary>
    /// Verifies that resolution rejects a null request.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestIsNull_ShouldThrowExactly()
    {
        NotableDateResolutionEngine engine = new(new StubOccurrenceResolver());

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            engine.Resolve(null!);
        });

        Assert.AreEqual("request", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the engine passes the original request to the occurrence resolver.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCalled_ShouldPassRequestToOccurrenceResolver()
    {
        StubOccurrenceResolver resolver = new();
        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: "AU");

        engine.Resolve(request);

        Assert.AreSame(request, resolver.LastRequest);
    }

    /// <summary>
    /// Verifies that an empty occurrence set resolves to an empty output set.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOccurrenceResolverReturnsNoOccurrences_ShouldReturnEmptyResult()
    {
        NotableDateResolutionEngine engine = new(new StubOccurrenceResolver());

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that emitted base occurrences are returned in chronological order.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOccurrencesAreOutOfOrder_ShouldReturnDatesInChronologicalOrder()
    {
        StubOccurrenceResolver resolver = new(
            Occurrence("Later Date", new DateTime(2024, 3, 1)),
            Occurrence("Earlier Date", new DateTime(2024, 2, 1)));

        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(2, actual.Count);
        Assert.AreEqual("Earlier Date", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 2, 1), actual[0].Date);
        Assert.AreEqual("Later Date", actual[1].Name);
        Assert.AreEqual(new DateTime(2024, 3, 1), actual[1].Date);
    }

    /// <summary>
    /// Verifies that observed-date projection includes multi-day occurrences whose span intersects the requested window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedProjectionIntersectsMultiDayOccurrence_ShouldReturnOccurrence()
    {
        StubOccurrenceResolver resolver = new(
            Occurrence(
                "Multi-Day Festival",
                new DateTime(2024, 6, 10),
                durationDays: 5));

        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Multi-Day Festival", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 6, 10), actual[0].Date);
        Assert.AreEqual(new DateTime(2024, 6, 14), actual[0].EndDate);
    }

    /// <summary>
    /// Verifies that anchor-date projection excludes occurrences whose anchor date is outside the requested window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorProjectionDoesNotContainAnchorDate_ShouldNotReturnOccurrence()
    {
        StubOccurrenceResolver resolver = new(
            Occurrence(
                "Multi-Day Festival",
                new DateTime(2024, 6, 10),
                durationDays: 5));

        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            NotableDateResolutionProjection.AnchorDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(0, actual.Count);
    }

    /// <summary>
    /// Verifies that anchor-date projection includes occurrences whose anchor date is inside the requested window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorProjectionContainsAnchorDate_ShouldReturnOccurrence()
    {
        StubOccurrenceResolver resolver = new(
            Occurrence(
                "Anchored Date",
                new DateTime(2024, 6, 10),
                anchorDate: new DateTime(2024, 6, 15)));

        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            NotableDateResolutionProjection.AnchorDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Anchored Date", actual[0].Name);
    }

    /// <summary>
    /// Verifies that non-working occurrence metadata is preserved in emitted output.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOccurrenceIsNonWorking_ShouldPreserveNonWorkingFlag()
    {
        StubOccurrenceResolver resolver = new(
            Occurrence(
                "Public Holiday",
                new DateTime(2024, 1, 26),
                nonWorking: true));

        NotableDateResolutionEngine engine = new(resolver);

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = engine.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.IsTrue(actual[0].IsNonWorkingDay);
    }

    private static ResolvedNotableDateOccurrence Occurrence(
        string name,
        DateTime date,
        bool nonWorking = false,
        int durationDays = 1,
        DateTime? anchorDate = null)
    {
        NotableDateRule rule = new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = date.Month,
            Day = date.Day,
            DurationDays = durationDays,
            IsNonWorkingDay = nonWorking,
        };

        NotableDate notable = new()
        {
            Name = name,
            Date = date.Date,
            Category = NotableDateCategory.Holiday,
            DurationDays = durationDays,
            IsNonWorkingDay = nonWorking,
        };

        return new ResolvedNotableDateOccurrence(
            rule,
            anchorDate?.Date ?? date.Date,
            TerritoryCode: null,
            notable);
    }

    private sealed class StubOccurrenceResolver
        : INotableDateRuleOccurrenceResolver
    {
        private readonly IReadOnlyList<ResolvedNotableDateOccurrence> occurrences;

        public StubOccurrenceResolver(params ResolvedNotableDateOccurrence[] occurrences)
        {
            this.occurrences = occurrences;
        }

        public NotableDateResolutionRequest? LastRequest { get; private set; }

        public IReadOnlyList<ResolvedNotableDateOccurrence> ResolveOccurrences(NotableDateResolutionRequest request)
        {
            LastRequest = request;

            return occurrences;
        }
    }
}
