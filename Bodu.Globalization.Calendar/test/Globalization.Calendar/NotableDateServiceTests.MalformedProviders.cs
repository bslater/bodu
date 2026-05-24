// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.MalformedProviders.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Globalization.Calendar.Plugins;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
    // =================================================================================================
    // Gap 1 — INotableDateRuleProvider.LoadRules() returns null
    // =================================================================================================

    /// <summary>
    /// Verifies that a rule provider whose <see cref="INotableDateRuleProvider.LoadRules" /> returns
    /// <see langword="null" /> instead of an empty enumerable is treated as contributing no rules, so the
    /// service constructs successfully and returns no notable dates rather than propagating a
    /// <see cref="NullReferenceException" /> to the caller.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProviderLoadRulesReturnsNull_ShouldTreatNullAsEmptyAndYieldNoNotableDates()
    {
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new NullReturningRuleProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(0, results.Count);
    }

    // =================================================================================================
    // Gap 2 — INotableDateRuleProvider.LoadRules() yields null rule elements
    // =================================================================================================

    /// <summary>
    /// Verifies that when a rule provider's <see cref="INotableDateRuleProvider.LoadRules" /> sequence
    /// contains a <see langword="null" /> element alongside valid rules, the null element is silently skipped
    /// and the valid rules are resolved normally, without surfacing a <see cref="NullReferenceException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProviderYieldsNullRuleElement_ShouldSkipNullsAndNotThrow()
    {
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new NullElementRuleProvider(sanity) },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Sanity Day", results[0].Name);
    }

    // =================================================================================================
    // Gap 3 — Fixed rule with invalid Gregorian month or day
    // =================================================================================================

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.Fixed" /> rule whose
    /// <see cref="NotableDateRule.Month" /> falls outside the valid range 1–12 does not cause
    /// <see cref="INotableDateService.GetNotableDates(int,string?,Type?)" /> to propagate an
    /// <see cref="ArgumentOutOfRangeException" />; the broken rule is silently omitted and sibling rules
    /// resolve normally.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFixedRuleHasInvalidGregorianMonth_ShouldOmitBrokenRuleAndNotThrow()
    {
        NotableDateRule badMonth = new()
        {
            Name = "Bad Month Rule",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 13,
            Day = 1,
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = BuildService(badMonth, sanity);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsFalse(results.Any(r => r.Name == "Bad Month Rule"), "A rule with Month=13 must be silently omitted.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "A broken rule must not suppress resolution of valid sibling rules.");
    }

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.Fixed" /> rule whose
    /// <see cref="NotableDateRule.Day" /> is invalid for the configured month does not propagate an
    /// <see cref="ArgumentOutOfRangeException" />; the broken rule is silently omitted and sibling rules
    /// resolve normally.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenFixedRuleHasInvalidGregorianDay_ShouldOmitBrokenRuleAndNotThrow()
    {
        NotableDateRule badDay = new()
        {
            Name = "Bad Day Rule",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 2,
            Day = 30,
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = BuildService(badDay, sanity);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsFalse(results.Any(r => r.Name == "Bad Day Rule"), "A rule with Day=30 in February must be silently omitted.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "A broken rule must not suppress resolution of valid sibling rules.");
    }

    // =================================================================================================
    // Gap 4 — OffsetFromAnchor with extreme OffsetDays that overflows DateTime
    // =================================================================================================

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule whose
    /// <see cref="NotableDateRule.OffsetDays" /> is so large that adding it to the anchor date would overflow
    /// <see cref="DateTime.MaxValue" /> does not propagate an <see cref="ArgumentOutOfRangeException" /> from
    /// <see cref="DateTime.AddDays(double)" />; the broken rule is silently omitted and sibling rules are
    /// unaffected.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOffsetFromAnchorProducesDateTimeOverflow_ShouldOmitRuleAndNotThrow()
    {
        NotableDateRule anchor = Fixed("Anchor Day", 1, 1);
        NotableDateRule overflow = new()
        {
            Name = "Overflow Offset",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Anchor Day",
            OffsetDays = int.MaxValue,
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = BuildService(anchor, overflow, sanity);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsFalse(results.Any(r => r.Name == "Overflow Offset"), "An overflowing offset rule must be silently omitted.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "An overflow in one rule must not suppress valid sibling rules.");
    }

    // =================================================================================================
    // Gap 5 — IAdjustmentHandler.Apply() returns null
    // =================================================================================================

    /// <summary>
    /// Verifies that when a custom <see cref="IAdjustmentHandler" /> returns <see langword="null" /> from
    /// <see cref="IAdjustmentHandler.Apply" />, the adjustment is treated as not activated and the service
    /// does not surface a <see cref="NullReferenceException" />; the base occurrence of the owning rule is
    /// still returned.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCustomAdjustmentHandlerApplyReturnsNull_ShouldTreatAsNotActivatedAndNotThrow()
    {
        NotableDateRule rule = Fixed("Target Holiday", 3, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "null-handler",
                Trigger = AdjustmentTrigger.Custom,
                Action = AdjustmentAction.Custom,
                HandlerKey = "null-handler",
            }),
        };

        AdjustmentHandlerRegistry handlerRegistry = new AdjustmentHandlerRegistry()
            .Register("null-handler", new NullResultHandler());

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { AdjustmentHandlers = handlerRegistry });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsTrue(results.Any(r => r.Name == "Target Holiday"), "The base occurrence must survive a null handler result.");
    }

    // =================================================================================================
    // Gap 6 — IAdjustmentHandler.Apply() throws a non-InvalidOperationException
    // =================================================================================================

    /// <summary>
    /// Verifies that when a custom <see cref="IAdjustmentHandler" /> throws an exception that is not an
    /// <see cref="InvalidOperationException" /> (such as <see cref="FormatException" />), the adjustment is
    /// treated as not activated rather than propagating the exception to the caller; the base occurrence of
    /// the owning rule and all sibling rules are still returned.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCustomAdjustmentHandlerThrowsNonInvalidOperationException_ShouldOmitAdjustmentAndNotThrow()
    {
        NotableDateRule rule = Fixed("Protected Holiday", 4, 1, nonWorking: true) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "throwing-handler",
                Trigger = AdjustmentTrigger.Custom,
                Action = AdjustmentAction.Custom,
                HandlerKey = "throwing-handler",
            }),
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        AdjustmentHandlerRegistry handlerRegistry = new AdjustmentHandlerRegistry()
            .Register("throwing-handler", new ThrowingHandler());

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule, sanity) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { AdjustmentHandlers = handlerRegistry });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsTrue(results.Any(r => r.Name == "Protected Holiday"), "The base occurrence must survive a throwing custom handler.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "A throwing handler in one rule must not suppress sibling rules.");
    }

    // =================================================================================================
    // Gap 7 — INotableDateAlgorithmRegistry.TryGet() returns true but sets algorithm to null
    // =================================================================================================

    /// <summary>
    /// Verifies that an <see cref="INotableDateAlgorithmRegistry" /> whose
    /// <see cref="INotableDateAlgorithmRegistry.TryGet" /> returns <see langword="true" /> but outputs a
    /// <see langword="null" /> algorithm does not cause a <see cref="NullReferenceException" /> when the
    /// service attempts to call <see cref="INotableDateAlgorithm.GetDate(int,Calendar?)" /> on the null
    /// reference; the associated rule is silently omitted and sibling rules resolve normally.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAlgorithmRegistryTryGetReturnsTrueWithNullAlgorithm_ShouldOmitRuleAndNotThrow()
    {
        NotableDateRule algorithmRule = new()
        {
            Name = "Null Algorithm Rule",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Observance,
            AlgorithmKey = "null-algorithm",
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(algorithmRule, sanity) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { AlgorithmRegistry = new NullAlgorithmRegistry() });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsFalse(results.Any(r => r.Name == "Null Algorithm Rule"), "A rule backed by a null algorithm must be silently omitted.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "A null algorithm must not suppress resolution of sibling rules.");
    }

    // =================================================================================================
    // Gap 8 — INotableDateCollisionResolver.Resolve() returns null
    // =================================================================================================

    /// <summary>
    /// Verifies that when <see cref="INotableDateCollisionResolver.Resolve" /> returns
    /// <see langword="null" />, <see cref="INotableDateService.GetNotableDates(DateTime,string?,Type?)" />
    /// does not propagate a <see cref="NullReferenceException" /> or return <see langword="null" /> to the
    /// caller; it returns an empty list, honouring the non-null return-type contract of
    /// <see cref="INotableDateService" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenCollisionResolverReturnsNull_ShouldReturnEmptyListAndNotThrow()
    {
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Any Holiday", 6, 15)) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { CollisionResolver = new NullReturningCollisionResolver() });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2025, 6, 15));

        Assert.IsNotNull(results, "GetNotableDates must never return null even when the collision resolver does.");
        Assert.AreEqual(0, results.Count);
    }

    // =================================================================================================
    // Gap 9 — INotableDateNameLocalizer.GetDisplayName() returns null
    // =================================================================================================

    /// <summary>
    /// Verifies that when <see cref="INotableDateNameLocalizer.GetDisplayName" /> returns
    /// <see langword="null" /> rather than a translated name, the service falls back to the original
    /// <see cref="NotableDate.Name" /> value rather than propagating <see langword="null" /> into the
    /// returned <see cref="NotableDate" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenNameLocalizerReturnsNull_ShouldFallBackToOriginalName()
    {
        NotableDateRule rule = Fixed("Christmas Day", 12, 25);

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { NameLocalizer = new DelegateLocaliser((_, _) => null!) });

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Christmas Day", results[0].Name, "Name must fall back to the rule's original name when the localizer returns null.");
    }

    // =================================================================================================
    // Gap 10 — INotableDateAlgorithm.GetDate() returns a date outside the queried year
    // =================================================================================================

    /// <summary>
    /// Verifies the current observable behaviour when an <see cref="INotableDateAlgorithm" /> returns a date
    /// whose year differs from the year requested. The range pipeline filters by observed date in the requested
    /// window: anchors whose observed date falls inside the window are emitted regardless of which input year the
    /// algorithm was asked about; anchors whose observed date falls outside are suppressed.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAlgorithmReturnsDateOutsideQueriedYear_ShouldFilterByObservedDateWindow()
    {
        NotableDateRule rule = new()
        {
            Name = "Cross-Year Date",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Observance,
            AlgorithmKey = "wrong-year",
        };

        var registry = new NotableDateAlgorithmRegistry(
            new[] { new KeyValuePair<string, INotableDateAlgorithm>("wrong-year", new WrongYearAlgorithm()) });

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { AlgorithmRegistry = registry });

        // WrongYearAlgorithm returns (year - 1, 12, 31). The pipeline materialises adjacent years' anchors and filters
        // by observed date in the queried window. Querying [2024-01-01, 2024-12-31] only — algorithm called for years
        // adjacent to the window returns 2023-12-31 and 2024-12-31; only the latter falls inside the window.
        IReadOnlyList<NotableDate> single = service.GetNotableDates(2024);
        Assert.AreEqual(1, single.Count);
        Assert.AreEqual(new DateTime(2024, 12, 31), single[0].Date,
            "Observed date inside the queried window survives; the algorithm-reported year is irrelevant once the date is materialised.");
    }

    // =================================================================================================
    // Gap 11 — GetNotableDates(DateTime, DateTime) across a very wide date range
    // =================================================================================================

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime,DateTime,string?,Type?)" />
    /// completes without exception or stack overflow when the supplied date range spans multiple decades,
    /// confirming that the year-iteration loop handles an extended but realistic range correctly.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDateRangeSpansMultipleDecades_ShouldCompleteAndNotThrow()
    {
        var service = BuildService(Fixed("Annual Day", 6, 15));

        IReadOnlyList<NotableDate> results = service.GetNotableDates(
            new DateTime(2000, 1, 1),
            new DateTime(2050, 12, 31));

        // 2000 through 2050 inclusive = 51 years, each producing one occurrence of "Annual Day".
        Assert.AreEqual(51, results.Count);
    }

    // =================================================================================================
    // Gap 12 — Plugin GetRuleProviders() or GetAlgorithms() returns null
    // =================================================================================================

    /// <summary>
    /// Verifies that a plugin whose <see cref="INotableDateRulePlugin.GetRuleProviders" /> returns
    /// <see langword="null" /> is treated as contributing no rule providers; the service constructs
    /// successfully without surfacing a <see cref="NullReferenceException" /> from the foreach loop that
    /// fans plugin providers into the effective provider list.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPluginGetRuleProvidersReturnsNull_ShouldTreatNullAsEmptyAndNotThrow()
    {
        var service = new NotableDateService(
            ruleProviders: Array.Empty<INotableDateRuleProvider>(),
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions { Plugins = new[] { (INotableDatePlugin)new NullProvidersRulePlugin() } });

        Assert.AreEqual(0, service.GetNotableDates(2025).Count);
    }

    /// <summary>
    /// Verifies that a plugin whose <see cref="INotableDateAlgorithmPlugin.GetAlgorithms" /> returns
    /// <see langword="null" /> is treated as contributing no algorithm registrations; the service constructs
    /// successfully without surfacing a <see cref="NullReferenceException" /> from the foreach loop that
    /// fans plugin algorithms into the effective registry.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPluginGetAlgorithmsReturnsNull_ShouldTreatNullAsEmptyAndNotThrow()
    {
        var service = new NotableDateService(
            ruleProviders: Array.Empty<INotableDateRuleProvider>(),
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions { Plugins = new[] { (INotableDatePlugin)new NullAlgorithmsPlugin() } });

        Assert.AreEqual(0, service.GetNotableDates(2025).Count);
    }

    // =================================================================================================
    // Gap 13 — INotableDateRuleOverrideProvider.GetRemovals() throws during construction
    // =================================================================================================

    /// <summary>
    /// Verifies that an exception raised by <see cref="INotableDateRuleOverrideProvider.GetRemovals" />
    /// during service construction propagates out of the constructor rather than being silently absorbed,
    /// so callers can observe and recover from a malfunctioning override provider without the service
    /// entering an inconsistent initialisation state.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOverrideProviderGetRemovalsThrows_ShouldPropagateExceptionFromConstructor()
    {
        var overrideProvider = new ThrowingRemovalsProvider(
            new InvalidOperationException("removal source unavailable"));

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new NotableDateService(
                new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Holiday", 1, 1)) },
                WorkingDaysOfWeek.MondayToFriday,
                new NotableDateServiceOptions
                {
                    OverrideProviders = new[] { (INotableDateRuleOverrideProvider)overrideProvider },
                });
        });

        Assert.IsTrue(ex.Message.Contains("removal source unavailable", StringComparison.Ordinal));
    }

    // =================================================================================================
    // Gap 14 — OffsetFromAnchor rule with a blank or whitespace AnchorRuleName
    // =================================================================================================

    /// <summary>
    /// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule whose
    /// <see cref="NotableDateRule.AnchorRuleName" /> is <see langword="null" />, empty, or whitespace is
    /// silently omitted from the year's results — the resolver's blank-anchor guard returns
    /// <see langword="null" /> rather than throwing — and sibling rules are unaffected.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOffsetFromAnchorRuleHasBlankAnchorRuleName_ShouldOmitRuleWithoutThrowing()
    {
        NotableDateRule blankAnchor = new()
        {
            Name = "Blank Anchor Rule",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "   ",
            OffsetDays = 1,
        };
        NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

        var service = BuildService(blankAnchor, sanity);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

        Assert.IsFalse(results.Any(r => r.Name == "Blank Anchor Rule"),
            "A rule with a whitespace-only AnchorRuleName must produce no occurrence.");
        Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"));
    }

    // =================================================================================================
    // Private helpers — malformed provider test infrastructure
    // =================================================================================================

    /// <summary>
    /// <see cref="INotableDateRuleProvider" /> that returns <see langword="null" /> from
    /// <see cref="LoadRules" />, simulating a provider that violates its contract by returning a null
    /// sequence rather than an empty one.
    /// </summary>
    private sealed class NullReturningRuleProvider
        : INotableDateRuleProvider
    {
        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => null!;
    }

    /// <summary>
    /// <see cref="INotableDateRuleProvider" /> that yields a <see langword="null" /> element first, then
    /// each supplied valid rule, simulating a partially corrupt enumerable.
    /// </summary>
    private sealed class NullElementRuleProvider
        : INotableDateRuleProvider
    {
        private readonly NotableDateRule[] _validRules;

        /// <summary>Initialises a new instance of <see cref="NullElementRuleProvider" />.</summary>
        /// <param name="validRules">The valid rules to yield after the leading null element.</param>
        public NullElementRuleProvider(params NotableDateRule[] validRules) => _validRules = validRules;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules()
        {
            yield return null!;
            foreach (NotableDateRule rule in _validRules)
                yield return rule;
        }
    }

    /// <summary>
    /// <see cref="INotableDateCollisionResolver" /> that always returns <see langword="null" /> from
    /// <see cref="Resolve" />, violating the interface contract that requires a non-null list.
    /// </summary>
    private sealed class NullReturningCollisionResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateTime date, IReadOnlyList<NotableDate> overlapping) => null!;
    }

    /// <summary>
    /// <see cref="INotableDateAlgorithmRegistry" /> that always claims to contain every key but returns
    /// <see langword="null" /> from <see cref="TryGet" />, violating the contract that a true return implies
    /// a non-null out value.
    /// </summary>
    private sealed class NullAlgorithmRegistry
        : INotableDateAlgorithmRegistry
    {
        /// <inheritdoc />
        public bool Contains(string key) => true;

        /// <inheritdoc />
        public bool TryGet(string key, out INotableDateAlgorithm algorithm)
        {
            algorithm = null!;
            return true;
        }
    }

    /// <summary>
    /// <see cref="INotableDateAlgorithm" /> that always returns a date in the year immediately preceding
    /// the requested year, exercising the path where an algorithm's result falls outside the queried year.
    /// </summary>
    private sealed class WrongYearAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null) =>
            new DateTime(year - 1, 12, 31);
    }

    /// <summary>
    /// <see cref="IAdjustmentHandler" /> that returns <see langword="null" /> from <see cref="Apply" />,
    /// violating the handler contract by returning a null result rather than an
    /// <see cref="AdjustmentHandlerResult" />.
    /// </summary>
    private sealed class NullResultHandler
        : IAdjustmentHandler
    {
        /// <inheritdoc />
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) => null!;
    }

    /// <summary>
    /// <see cref="IAdjustmentHandler" /> that throws a <see cref="FormatException" /> from
    /// <see cref="Apply" />, simulating a handler that fails with an unexpected exception type that is
    /// not caught by the service's <see cref="InvalidOperationException" />-only rule boundary.
    /// </summary>
    private sealed class ThrowingHandler
        : IAdjustmentHandler
    {
        /// <inheritdoc />
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
            throw new FormatException("handler internal failure");
    }

    /// <summary>
    /// Plugin implementing <see cref="INotableDateRulePlugin" /> whose <see cref="GetRuleProviders" />
    /// returns <see langword="null" /> rather than an empty sequence.
    /// </summary>
    private sealed class NullProvidersRulePlugin
        : INotableDateRulePlugin
    {
        /// <inheritdoc />
        public string Name => "NullProviders";

        /// <inheritdoc />
        public Version Version => new(1, 0);

        /// <inheritdoc />
        public IEnumerable<INotableDateRuleProvider> GetRuleProviders() => null!;
    }

    /// <summary>
    /// Plugin implementing <see cref="INotableDateAlgorithmPlugin" /> whose <see cref="GetAlgorithms" />
    /// returns <see langword="null" /> rather than an empty sequence.
    /// </summary>
    private sealed class NullAlgorithmsPlugin
        : INotableDateAlgorithmPlugin
    {
        /// <inheritdoc />
        public string Name => "NullAlgorithms";

        /// <inheritdoc />
        public Version Version => new(1, 0);

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms() => null!;
    }

    /// <summary>
    /// <see cref="INotableDateRuleOverrideProvider" /> whose <see cref="GetRemovals" /> method always
    /// throws the supplied exception, verifying that override-provider failures surface from the service
    /// constructor rather than being silently swallowed.
    /// </summary>
    private sealed class ThrowingRemovalsProvider
        : INotableDateRuleOverrideProvider
    {
        private readonly Exception _exception;

        /// <summary>Initialises a new instance of <see cref="ThrowingRemovalsProvider" />.</summary>
        /// <param name="exception">The exception to raise on every <see cref="GetRemovals" /> call.</param>
        public ThrowingRemovalsProvider(Exception exception) => _exception = exception;

        /// <inheritdoc />
        public IEnumerable<RuleRemoval> GetRemovals() => throw _exception;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> GetAdditions() => Array.Empty<NotableDateRule>();
    }
}
