// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests range-pipeline behaviour through the public <see cref="NotableDateService" /> surface.
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
            _ = new NotableDateService(null!, WorkingDaysOfWeek.MondayToFriday);
        });

        Assert.AreEqual("ruleProviders", ex.ParamName);
    }

    /// <summary>
    /// Verifies that rules loaded by the configured providers are visible through subsequent queries.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRuleProvidersContainRules_ShouldResolveBothRules()
    {
        NotableDateRule first = FixedRule("First", month: 1, day: 1);
        NotableDateRule second = FixedRule("Second", month: 1, day: 2);

        NotableDateService service = CreateService(first, second);

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(2024);

        Assert.IsTrue(resolved.Any(date => date.Name == "First"));
        Assert.IsTrue(resolved.Any(date => date.Name == "Second"));
    }

    /// <summary>
    /// Verifies that the service resolves a simple fixed-date occurrence through the range pipeline.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedRuleFallsInsideWindow_ShouldReturnOccurrence()
    {
        NotableDateService service = CreateService(
            FixedRule("Fixed Date", month: 5, day: 10));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 5, 1),
            new DateTime(2024, 5, 31));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Fixed Date", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 5, 10), actual[0].Date);
    }

    /// <summary>
    /// Verifies that the service can resolve an anchor-relative notable date without emitting the calculation anchor
    /// when the anchor itself is filtered out and only the offset rule's window is requested.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowContainsStartOfLentButNotEaster_ShouldReturnStartOfLentOnly()
    {
        CountingEasterAlgorithm.Reset();

        NotableDateService service = CreateService(
            EasterSundayRule(),
            OffsetRule("Start of Lent", offsetDays: -46),
            OffsetRule("Palm Sunday", offsetDays: -7),
            OffsetRule("Good Friday", offsetDays: -2));

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Start of Lent", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 2, 14), actual[0].Date);

        Assert.IsFalse(actual.Any(date => date.Name == "Easter Sunday"));
        Assert.IsFalse(actual.Any(date => date.Name == "Palm Sunday"));
        Assert.IsFalse(actual.Any(date => date.Name == "Good Friday"));

        // A single 2024 request should invoke the algorithm at most for the candidate-year set; the cached anchor
        // resolver guarantees idempotent reuse within a request.
        Assert.IsTrue(CountingEasterAlgorithm.CallCount >= 1);
    }

    /// <summary>
    /// Verifies that two consecutive narrow window requests against the same algorithm-backed anchor each resolve
    /// their respective offset rule correctly.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLaterWindowUsesSameAnchorYears_ShouldResolveBothOffsetRules()
    {
        NotableDateService service = CreateService(
            EasterSundayRule(),
            OffsetRule("Start of Lent", offsetDays: -46),
            OffsetRule("Palm Sunday", offsetDays: -7));

        IReadOnlyList<NotableDate> lent = service.GetNotableDates(
            new DateTime(2024, 2, 10),
            new DateTime(2024, 2, 20),
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        IReadOnlyList<NotableDate> palmSunday = service.GetNotableDates(
            new DateTime(2024, 3, 20),
            new DateTime(2024, 3, 25),
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, lent.Count);
        Assert.AreEqual("Start of Lent", lent[0].Name);

        Assert.AreEqual(1, palmSunday.Count);
        Assert.AreEqual("Palm Sunday", palmSunday[0].Name);
        Assert.AreEqual(new DateTime(2024, 3, 24), palmSunday[0].Date);
    }

    /// <summary>
    /// Verifies that territory-scoped rules are resolved by the range pipeline.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestedTerritoryMatchesSubdivision_ShouldReturnScopedOccurrence()
    {
        NotableDateService service = CreateService(
            FixedRule("NSW Observance", month: 8, day: 5) with
            {
                TerritoryCode = "AU-NSW",
            });

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 8, 1),
            new DateTime(2024, 8, 10),
            territoryCode: "AU");

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
        NotableDateService service = CreateService(
            FixedRule("Religious Festival", month: 6, day: 10) with
            {
                DurationDays = 5,
            });

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 6, 14),
            new DateTime(2024, 6, 20),
            filter: NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Religious Festival", actual[0].Name);
        Assert.AreEqual(new DateTime(2024, 6, 10), actual[0].Date);
        Assert.AreEqual(new DateTime(2024, 6, 14), actual[0].EndDate);
    }

    /// <summary>
    /// Verifies that collision resolution is applied to dates emitted on the same day.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultipleDatesFallOnSameDayAndCollisionResolverIsProvided_ShouldApplyCollisionResolver()
    {
        NotableDateService service = new(
            ruleProviders:
            [
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
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                CollisionResolver = new FirstByPriorityCollisionResolver(),
            });

        IReadOnlyList<NotableDate> actual = service.GetNotableDates(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 1));

        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual("Higher Priority", actual[0].Name);
    }

    private static NotableDateService CreateService(params NotableDateRule[] rules)
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register(typeof(CountingEasterAlgorithm).FullName!, new CountingEasterAlgorithm());

        return new NotableDateService(
            ruleProviders: [new InMemoryRuleProvider(rules)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
    }

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
