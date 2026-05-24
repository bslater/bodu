// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateResolutionService" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceTests
{
    /// <summary>
    /// Verifies that construction rejects a null rule-provider sequence.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProvidersIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateResolutionService(null!);
        });

        Assert.AreEqual("ruleProviders", ex.ParamName);
    }

    /// <summary>
    /// Verifies that resolution rejects a null request.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestIsNull_ShouldThrowExactly()
    {
        NotableDateResolutionService service = CreateService();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            service.Resolve(null!);
        });

        Assert.AreEqual("request", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the service loads rules from its providers.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProvidersContainRules_ShouldExposeEffectiveRules()
    {
        NotableDateRule first = FixedRule("First", month: 1, day: 1);
        NotableDateRule second = FixedRule("Second", month: 1, day: 2);

        NotableDateResolutionService service = CreateService(first, second);

        Assert.AreEqual(2, service.EffectiveRules.Count);
        Assert.IsTrue(service.EffectiveRules.Any(rule => rule.Name == "First"));
        Assert.IsTrue(service.EffectiveRules.Any(rule => rule.Name == "Second"));
    }

    /// <summary>
    /// Verifies that the service resolves a simple fixed-date occurrence through the new pipeline.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedRuleFallsInsideWindow_ShouldReturnOccurrence()
    {
        NotableDateResolutionService service = CreateService(
            FixedRule("Fixed Date", month: 5, day: 10));

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 5, 1),
            new DateTime(2024, 5, 31),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Fixed Date", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 5, 10), actual[0].Date);
    }

    /// <summary>
    /// Verifies that the service can resolve an anchor-relative notable date without emitting the calculation anchor.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowContainsStartOfLentButNotEaster_ShouldReturnStartOfLentOnly()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateResolutionService service = CreateService(
            EasterSundayRule(),
            OffsetRule("Start of Lent", offsetDays: -46),
            OffsetRule("Palm Sunday", offsetDays: -7),
            OffsetRule("Good Friday", offsetDays: -2));

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Start of Lent", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 2, 14), actual[0].Date);

        Assert.IsFalse(actual.Any(date => date.Name == "Easter Sunday"));
        Assert.IsFalse(actual.Any(date => date.Name == "Palm Sunday"));
        Assert.IsFalse(actual.Any(date => date.Name == "Good Friday"));

        // The service uses a tight candidate-year envelope ([request.Year]); a single 2024 request invokes the algorithm once.
        Assert.AreEqual(1, CountingEasterAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that a later request reuses the same cached calculation anchors.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLaterWindowUsesSameAnchorYears_ShouldReuseCachedCalculationAnchors()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateResolutionService service = CreateService(
            EasterSundayRule(),
            OffsetRule("Start of Lent", offsetDays: -46),
            OffsetRule("Palm Sunday", offsetDays: -7));

        NotableDateResolutionRequest lentRequest = new(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        NotableDateResolutionRequest palmSundayRequest = new(
            new DateTime(2024, 3, 20),
            new DateTime(2024, 3, 25),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> lent = service.Resolve(lentRequest);
        IReadOnlyList<NotableDate> palmSunday = service.Resolve(palmSundayRequest);

        Assert.AreEqual(1, lent.Count);
        Assert.AreEqual("Start of Lent", lent[0].Name);

        Assert.AreEqual(1, palmSunday.Count);
        Assert.AreEqual("Palm Sunday", palmSunday[0].Name);
        Assert.AreEqual(new DateTime(2024, 3, 24), palmSunday[0].Date);

        // With the tight [request.Year] candidate-year envelope, both requests target Easter 2024 only, so the second request
        // reuses the cached anchor and the algorithm runs exactly once across the two calls.
        Assert.AreEqual(1, CountingEasterAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that territory-scoped rules are resolved by the new service.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestedTerritoryMatchesSubdivision_ShouldReturnScopedOccurrence()
    {
        NotableDateResolutionService service = CreateService(
            FixedRule("NSW Observance", month: 8, day: 5) with
            {
                TerritoryCode = "AU-NSW",
            });

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 8, 1),
            new DateTime(2024, 8, 10),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: "AU");

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("NSW Observance", actual[0].Name);
        Assert.AreEqual("AU-NSW", actual[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that multi-day occurrences are returned when their span intersects the requested window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDayOccurrenceIntersectsWindow_ShouldReturnOccurrence()
    {
        NotableDateResolutionService service = CreateService(
            FixedRule("Religious Festival", month: 6, day: 10) with
            {
                DurationDays = 5,
            });

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            NotableDateResolutionProjection.ObservedDate,
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Religious Festival", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 6, 10), actual[0].Date);
        Assert.AreEqual(new DateTime(2024, 6, 14), actual[0].EndDate);
    }

    /// <summary>
    /// Verifies that collision resolution is applied by the service facade.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultipleDatesFallOnSameDayAndCollisionResolverIsProvided_ShouldApplyCollisionResolver()
    {
        NotableDateResolutionService service = new(
            ruleProviders: new[]
            {
                new InMemoryRuleProvider(
                [
                    FixedRule("Lower Priority", month: 1, day: 1) with
                    {
                        Priority = 20,
                    },
                    FixedRule("Higher Priority", month: 1, day: 1) with
                    {
                        Priority = 10,
                    },
                ]),
            },
            collisionResolver: new FirstByPriorityCollisionResolver());

        NotableDateResolutionRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 1),
            NotableDateResolutionProjection.ObservedDate);

        IReadOnlyList<NotableDate> actual = service.Resolve(request);

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Higher Priority", actual[0].Name);
    }

    private static NotableDateResolutionService CreateService(params NotableDateRule[] rules) =>
        new(ruleProviders: new[] { new InMemoryRuleProvider(rules) });

    private static NotableDateRule EasterSundayRule() =>
        new()
        {
            Name = "Easter Sunday",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmType = typeof(CountingEasterAlgorithm),
            IsNonWorkingDay = false,
            Tags = ImmutableHashSet.Create("Christian"),
        };

    private static NotableDateRule OffsetRule(string name, int offsetDays) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Religious,
            AnchorRuleName = "Easter Sunday",
            OffsetDays = offsetDays,
            IsNonWorkingDay = false,
            Tags = ImmutableHashSet.Create("Christian"),
        };

    private static NotableDateRule FixedRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Religious,
            Month = month,
            Day = day,
            IsNonWorkingDay = false,
        };

    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        public InMemoryRuleProvider(IReadOnlyList<NotableDateRule> rules)
        {
            this._rules = rules;
        }

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }

    private sealed class CountingEasterAlgorithm
        : INotableDateAlgorithm
    {
        public static int CallCount { get; private set; }

        public static void Reset() => CallCount = 0;

        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
        {
            CallCount++;

            return year switch
            {
                2023 => new DateTime(2023, 4, 9),
                2024 => new DateTime(2024, 3, 31),
                2025 => new DateTime(2025, 4, 20),
                _ => null,
            };
        }
    }

    private sealed class FirstByPriorityCollisionResolver
        : INotableDateCollisionResolver
    {
        public IReadOnlyList<NotableDate> Resolve(DateTime date, IReadOnlyList<NotableDate> candidates) =>
            candidates
                .OrderBy(candidate => candidate.Name == "Higher Priority" ? 0 : 1)
                .Take(1)
                .ToList();
    }
}
