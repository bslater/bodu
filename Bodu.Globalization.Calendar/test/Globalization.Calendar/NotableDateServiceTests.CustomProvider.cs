// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.CustomProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that a single custom <see cref="INotableDateRuleProvider" /> contributes every rule it loads to the service's
    /// resolved output.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleCustomProviderSupplied_ShouldExposeItsRules()
    {
        var provider = new InMemoryRuleProvider(
            Fixed("Custom Holiday", 3, 15),
            Fixed("Custom Observance", 7, 20, NotableDateCategory.Observance));

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Name == "Custom Holiday"));
        Assert.IsTrue(results.Any(r => r.Name == "Custom Observance"));
    }

    /// <summary>
    /// Verifies that rules supplied by multiple custom <see cref="INotableDateRuleProvider" /> instances are aggregated into a
    /// single resolved result set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMultipleCustomProvidersSupplied_ShouldAggregateRulesFromAllProviders()
    {
        var first = new InMemoryRuleProvider(Fixed("Holiday A", 1, 1));
        var second = new InMemoryRuleProvider(
            Fixed("Holiday B", 6, 1),
            Fixed("Holiday C", 12, 25));

        var service = new NotableDateService(
            new INotableDateRuleProvider[] { first, second },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results.Any(r => r.Name == "Holiday A"));
        Assert.IsTrue(results.Any(r => r.Name == "Holiday B"));
        Assert.IsTrue(results.Any(r => r.Name == "Holiday C"));
    }

    /// <summary>
    /// Verifies that an empty provider sequence is accepted and yields no notable dates for any queried year.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderSequenceIsEmpty_ShouldYieldNoNotableDates()
    {
        var service = new NotableDateService(
            Array.Empty<INotableDateRuleProvider>(),
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that a custom provider returning an empty rule sequence is a benign no-op and produces no resolved dates.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomProviderYieldsNoRules_ShouldYieldNoNotableDates()
    {
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that a deferred iterator-based provider is fully enumerated during construction, so that later mutation of the
    /// backing rule source cannot leak into resolved results.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomProviderReturnsDeferredEnumerable_ShouldMaterialiseRulesImmediately()
    {
        List<NotableDateRule> source = new() { Fixed("Initial", 1, 1) };
        var provider = new DeferredRuleProvider(() => EnumerateRules(source));

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        source.Add(Fixed("Added Later", 2, 2));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Initial", results[0].Name);
    }

    /// <summary>
    /// Verifies that an exception raised by <see cref="INotableDateRuleProvider.LoadRules" /> propagates out of the service
    /// constructor rather than being deferred until the first query.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomProviderThrows_ShouldSurfaceExceptionFromConstructor()
    {
        var provider = new ThrowingRuleProvider(new InvalidOperationException("broken"));

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new NotableDateService(
                new[] { (INotableDateRuleProvider)provider },
                WorkingDaysOfWeek.MondayToFriday);
        });

        Assert.AreEqual("broken", ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateRuleProvider.LoadRules" /> is invoked exactly once per provider during service
    /// construction, irrespective of how many years are subsequently queried.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenServiceQueriedRepeatedly_ShouldBeInvokedOncePerProvider()
    {
        var provider = new CountingRuleProvider(Fixed("Holiday", 1, 1));

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        _ = service.GetNotableDates(2024);
        _ = service.GetNotableDates(2025);
        _ = service.GetNotableDates(2026);

        Assert.AreEqual(1, provider.LoadRulesCallCount);
    }

    /// <summary>
    /// Verifies that each custom provider is enumerated exactly once when several providers are supplied to the service.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenMultipleCustomProvidersSupplied_ShouldInvokeEachProviderExactlyOnce()
    {
        var first = new CountingRuleProvider(Fixed("Holiday A", 1, 1));
        var second = new CountingRuleProvider(Fixed("Holiday B", 2, 2));

        var service = new NotableDateService(
            new INotableDateRuleProvider[] { first, second },
            WorkingDaysOfWeek.MondayToFriday);

        _ = service.GetNotableDates(2025);

        Assert.AreEqual(1, first.LoadRulesCallCount);
        Assert.AreEqual(1, second.LoadRulesCallCount);
    }

    /// <summary>
    /// Verifies that clearing the per-year cache via <see cref="NotableDateService.Invalidate()" /> and
    /// <see cref="NotableDateService.Invalidate(int)" /> does not trigger a further call to
    /// <see cref="INotableDateRuleProvider.LoadRules" />; the loaded rule set is retained across invalidations.
    /// </summary>
    [TestMethod]
    public void Invalidate_WhenServiceUsesCustomProvider_ShouldNotReloadProviderRules()
    {
        var provider = new CountingRuleProvider(Fixed("Holiday", 1, 1));

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        _ = service.GetNotableDates(2025);
        service.Invalidate();
        _ = service.GetNotableDates(2025);
        service.Invalidate(2025);
        _ = service.GetNotableDates(2025);

        Assert.AreEqual(1, provider.LoadRulesCallCount);
    }

    /// <summary>
    /// Verifies that a rule emitted by a custom provider participates in the territory filter pipeline exactly like any built-in
    /// rule, with parent-contains-subdivision semantics.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCustomProviderRuleIsScopedToTerritory_ShouldHonourTerritoryFilter()
    {
        var provider = new InMemoryRuleProvider(
            Fixed("Regional Holiday", 6, 6, territory: "AU-NSW"));

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> nsw = service.GetNotableDates(2025, territoryCode: "AU-NSW");
        IReadOnlyList<NotableDate> qld = service.GetNotableDates(2025, territoryCode: "AU-QLD");
        IReadOnlyList<NotableDate> au = service.GetNotableDates(2025, territoryCode: "AU");

        Assert.AreEqual(1, nsw.Count);
        Assert.AreEqual(0, qld.Count);
        Assert.AreEqual(1, au.Count);
    }

    /// <summary>
    /// Verifies that an <see cref="INotableDateRuleOverrideProvider" /> suppressing a rule supplied by a custom base
    /// <see cref="INotableDateRuleProvider" /> correctly removes that rule from the resolved output.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOverrideSuppressesCustomProviderRule_ShouldExcludeRule()
    {
        var baseProvider = new InMemoryRuleProvider(Fixed("Custom Holiday", 4, 1));
        var overrideProvider = new TestOverrideProvider(
            removals: new[] { new RuleRemoval("Custom Holiday") },
            additions: Array.Empty<NotableDateRule>());

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)baseProvider },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions
            {
                OverrideProviders = new[] { (INotableDateRuleOverrideProvider)overrideProvider },
            });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule supplied by one custom provider resolves
    /// correctly against an anchor rule contributed by a separate custom provider in the same service.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAnchorAndOffsetRulesAreInDistinctProviders_ShouldResolveAcrossProviders()
    {
        var anchorProvider = new InMemoryRuleProvider(Fixed("Anchor Day", 5, 1));
        NotableDateRule offsetRule = new()
        {
            Name = "Day After Anchor",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Anchor Day",
            OffsetDays = 1,
        };
        var offsetProvider = new InMemoryRuleProvider(offsetRule);

        var service = new NotableDateService(
            new INotableDateRuleProvider[] { anchorProvider, offsetProvider },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);
        NotableDate offset = results.Single(r => r.Name == "Day After Anchor");

        Assert.AreEqual(new DateTime(2025, 5, 2), offset.Date);
    }

    /// <summary>
    /// Verifies that applicability gates (<see cref="NotableDateRule.FirstYear" /> and <see cref="NotableDateRule.LastYear" />)
    /// authored by a custom provider are honoured by the service's per-year generation pipeline.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCustomProviderRuleHasYearBounds_ShouldApplyYearGating()
    {
        NotableDateRule bounded = Fixed("Bounded Holiday", 5, 1) with
        {
            FirstYear = 2024,
            LastYear = 2025,
        };
        var provider = new InMemoryRuleProvider(bounded);

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)provider },
            WorkingDaysOfWeek.MondayToFriday);

        Assert.AreEqual(0, service.GetNotableDates(2023).Count);
        Assert.AreEqual(1, service.GetNotableDates(2024).Count);
        Assert.AreEqual(1, service.GetNotableDates(2025).Count);
        Assert.AreEqual(0, service.GetNotableDates(2026).Count);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateRuleOverrideProvider.GetRemovals" /> is invoked exactly once per provider during
    /// service construction and is not re-enumerated on subsequent queries. The service materialises an internal removals
    /// snapshot so that non-trivial override providers (database-backed, lazily-enumerated, or expensive to compute) cannot drive
    /// per-rule × per-year × per-territory traffic during query operations.
    /// </summary>
    [TestMethod]
    public void GetRemovals_WhenServiceQueriedRepeatedly_ShouldBeInvokedOncePerOverrideProvider()
    {
        var baseProvider = new InMemoryRuleProvider(Fixed("Holiday A", 1, 1, territory: "AU-NSW"));
        var override_ = new CountingOverrideProvider(
            removals: new[] { new RuleRemoval("Holiday A", TerritoryCode: "AU-NSW") },
            additions: Array.Empty<NotableDateRule>());

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)baseProvider },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions
            {
                OverrideProviders = new[] { (INotableDateRuleOverrideProvider)override_ },
            });

        _ = service.GetNotableDates(2024, territoryCode: "AU-NSW");
        _ = service.GetNotableDates(2025, territoryCode: "AU-NSW");
        _ = service.GetNotableDates(2026, territoryCode: "AU-NSW");
        service.Invalidate();
        _ = service.GetNotableDates(2025, territoryCode: "AU-NSW");

        Assert.AreEqual(1, override_.GetRemovalsCallCount);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateRuleOverrideProvider.GetAdditions" /> is invoked exactly once per provider during
    /// service construction. Together with the corresponding removals guarantee, this pins the override-provider call count to a
    /// constant per service lifetime regardless of query volume or cache invalidations.
    /// </summary>
    [TestMethod]
    public void GetAdditions_WhenServiceQueriedRepeatedly_ShouldBeInvokedOncePerOverrideProvider()
    {
        var baseProvider = new InMemoryRuleProvider(Fixed("Holiday A", 1, 1));
        var override_ = new CountingOverrideProvider(
            removals: Array.Empty<RuleRemoval>(),
            additions: new[] { Fixed("One-Off", 6, 15) });

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)baseProvider },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions
            {
                OverrideProviders = new[] { (INotableDateRuleOverrideProvider)override_ },
            });

        _ = service.GetNotableDates(2024);
        _ = service.GetNotableDates(2025);
        service.Invalidate();
        _ = service.GetNotableDates(2025);

        Assert.AreEqual(1, override_.GetAdditionsCallCount);
    }

    /// <summary>
    /// <see cref="INotableDateRuleProvider" /> that records the number of times <see cref="LoadRules" /> is invoked, enabling
    /// tests to assert load-once semantics across multiple service queries and cache invalidations.
    /// </summary>
    private sealed class CountingRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        /// <summary>
        /// Initialises a new instance of the <see cref="CountingRuleProvider" /> class.
        /// </summary>
        /// <param name="rules">The rules to return on every call to <see cref="LoadRules" />.</param>
        public CountingRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        /// <summary>
        /// Gets the number of times <see cref="LoadRules" /> has been invoked since construction.
        /// </summary>
        /// <returns>The invocation count.</returns>
        public int LoadRulesCallCount { get; private set; }

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules()
        {
            LoadRulesCallCount++;
            return _rules;
        }
    }

    /// <summary>
    /// <see cref="INotableDateRuleProvider" /> that always throws a supplied exception, used to verify that provider failures
    /// surface from the service constructor rather than being silently absorbed.
    /// </summary>
    private sealed class ThrowingRuleProvider
        : INotableDateRuleProvider
    {
        private readonly Exception _exception;

        /// <summary>
        /// Initialises a new instance of the <see cref="ThrowingRuleProvider" /> class.
        /// </summary>
        /// <param name="exception">The exception to raise when <see cref="LoadRules" /> is invoked.</param>
        public ThrowingRuleProvider(Exception exception) => _exception = exception;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => throw _exception;
    }

    /// <summary>
    /// <see cref="INotableDateRuleProvider" /> whose rule production is deferred until <see cref="LoadRules" /> is invoked, so
    /// tests can confirm that the service materialises provider output at construction time.
    /// </summary>
    private sealed class DeferredRuleProvider
        : INotableDateRuleProvider
    {
        private readonly Func<IEnumerable<NotableDateRule>> _factory;

        /// <summary>
        /// Initialises a new instance of the <see cref="DeferredRuleProvider" /> class.
        /// </summary>
        /// <param name="factory">The delegate invoked to obtain the rule sequence on each call to <see cref="LoadRules" />.</param>
        public DeferredRuleProvider(Func<IEnumerable<NotableDateRule>> factory) => _factory = factory;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => _factory();
    }

    /// <summary>
    /// Lazily yields each rule from <paramref name="source" />, producing a genuine iterator whose enumeration is deferred until
    /// the consumer pulls items.
    /// </summary>
    /// <param name="source">The backing rule sequence.</param>
    /// <returns>An iterator over <paramref name="source" />.</returns>
    private static IEnumerable<NotableDateRule> EnumerateRules(IEnumerable<NotableDateRule> source)
    {
        foreach (NotableDateRule rule in source)
            yield return rule;
    }

    /// <summary>
    /// <see cref="INotableDateRuleOverrideProvider" /> that records the number of times <see cref="GetRemovals" /> and
    /// <see cref="GetAdditions" /> are invoked, used to verify that the service materialises override-provider output once at
    /// construction rather than re-enumerating on every query.
    /// </summary>
    private sealed class CountingOverrideProvider
        : INotableDateRuleOverrideProvider
    {
        private readonly IReadOnlyList<RuleRemoval> _removals;
        private readonly IReadOnlyList<NotableDateRule> _additions;

        /// <summary>
        /// Initialises a new instance of the <see cref="CountingOverrideProvider" /> class.
        /// </summary>
        /// <param name="removals">The removals to return on every <see cref="GetRemovals" /> call.</param>
        /// <param name="additions">The additions to return on every <see cref="GetAdditions" /> call.</param>
        public CountingOverrideProvider(IEnumerable<RuleRemoval> removals, IEnumerable<NotableDateRule> additions)
        {
            _removals = removals.ToList();
            _additions = additions.ToList();
        }

        /// <summary>
        /// Gets the number of times <see cref="GetRemovals" /> has been invoked since construction.
        /// </summary>
        /// <returns>The invocation count.</returns>
        public int GetRemovalsCallCount { get; private set; }

        /// <summary>
        /// Gets the number of times <see cref="GetAdditions" /> has been invoked since construction.
        /// </summary>
        /// <returns>The invocation count.</returns>
        public int GetAdditionsCallCount { get; private set; }

        /// <inheritdoc />
        public IEnumerable<RuleRemoval> GetRemovals()
        {
            GetRemovalsCallCount++;
            return _removals;
        }

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> GetAdditions()
        {
            GetAdditionsCallCount++;
            return _additions;
        }
    }
}
