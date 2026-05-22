// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingCalculationAnchorResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="CachingCalculationAnchorResolver" /> class.
/// </summary>
[TestClass]
public sealed class CachingCalculationAnchorResolverTests
{
    /// <summary>
    /// Verifies that resolving the same anchor rule for the same year uses the cached value after the first calculation.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameAnchorAndYearRequestedMultipleTimes_ShouldCalculateAnchorOnlyOnce()
    {
        CountingAlgorithm.Reset();

        NotableDateRule easterSunday = AlgorithmRule(
            "Easter Sunday",
            typeof(CountingAlgorithm));

        NotableDateRuleResolver ruleResolver = new([easterSunday]);
        CachingCalculationAnchorResolver resolver = new([easterSunday], ruleResolver);

        DateTime? first = resolver.Resolve("Easter Sunday", 2024);
        DateTime? second = resolver.Resolve("Easter Sunday", 2024);

        Assert.AreEqual(new DateTime(2024, 3, 31), first);
        Assert.AreEqual(first, second);
        Assert.AreEqual(1, CountingAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that the cache key includes the civil year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameAnchorRequestedForDifferentYears_ShouldCalculateEachYearSeparately()
    {
        CountingAlgorithm.Reset();

        NotableDateRule easterSunday = AlgorithmRule(
            "Easter Sunday",
            typeof(CountingAlgorithm));

        NotableDateRuleResolver ruleResolver = new([easterSunday]);
        CachingCalculationAnchorResolver resolver = new([easterSunday], ruleResolver);

        DateTime? first = resolver.Resolve("Easter Sunday", 2024);
        DateTime? second = resolver.Resolve("Easter Sunday", 2025);

        Assert.AreEqual(new DateTime(2024, 3, 31), first);
        Assert.AreEqual(new DateTime(2025, 3, 31), second);
        Assert.AreEqual(2, CountingAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that an anchor rule that produces no date is cached as a resolved absence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorReturnsNull_ShouldCacheNullResult()
    {
        NullReturningAlgorithm.Reset();

        NotableDateRule anchor = AlgorithmRule(
            "Optional Anchor",
            typeof(NullReturningAlgorithm));

        NotableDateRuleResolver ruleResolver = new([anchor]);
        CachingCalculationAnchorResolver resolver = new([anchor], ruleResolver);

        DateTime? first = resolver.Resolve("Optional Anchor", 2024);
        DateTime? second = resolver.Resolve("Optional Anchor", 2024);

        Assert.IsNull(first);
        Assert.IsNull(second);
        Assert.AreEqual(1, NullReturningAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that failed anchor calculations are not permanently cached.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorCalculationThrows_ShouldNotCacheFailure()
    {
        ThrowOnceAlgorithm.Reset();

        NotableDateRule anchor = AlgorithmRule(
            "Unstable Anchor",
            typeof(ThrowOnceAlgorithm));

        NotableDateRuleResolver ruleResolver = new([anchor]);
        CachingCalculationAnchorResolver resolver = new([anchor], ruleResolver);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            resolver.Resolve("Unstable Anchor", 2024);
        });

        DateTime? actual = resolver.Resolve("Unstable Anchor", 2024);

        Assert.AreEqual(new DateTime(2024, 3, 31), actual);
        Assert.AreEqual(2, ThrowOnceAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that anchor rule name lookups are case-insensitive.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorNameUsesDifferentCasing_ShouldResolveAnchor()
    {
        CountingAlgorithm.Reset();

        NotableDateRule easterSunday = AlgorithmRule(
            "Easter Sunday",
            typeof(CountingAlgorithm));

        NotableDateRuleResolver ruleResolver = new([easterSunday]);
        CachingCalculationAnchorResolver resolver = new([easterSunday], ruleResolver);

        DateTime? actual = resolver.Resolve("easter sunday", 2024);

        Assert.AreEqual(new DateTime(2024, 3, 31), actual);
        Assert.AreEqual(1, CountingAlgorithm.CallCount);
    }

    /// <summary>
    /// Verifies that an unknown anchor name fails clearly.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnchorRuleDoesNotExist_ShouldThrowExactly()
    {
        NotableDateRule easterSunday = AlgorithmRule(
            "Easter Sunday",
            typeof(CountingAlgorithm));

        NotableDateRuleResolver ruleResolver = new([easterSunday]);
        CachingCalculationAnchorResolver resolver = new([easterSunday], ruleResolver);

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            resolver.Resolve("Missing Anchor", 2024);
        });

        StringAssert.Contains(ex.Message, "Missing Anchor");
    }

    /// <summary>
    /// Verifies that invalid anchor names fail before resolution.
    /// </summary>
    /// <param name="anchorRuleName">The invalid anchor rule name.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Resolve_WhenAnchorRuleNameIsNullOrWhiteSpace_ShouldThrowExactly(string? anchorRuleName)
    {
        NotableDateRule easterSunday = AlgorithmRule(
            "Easter Sunday",
            typeof(CountingAlgorithm));

        NotableDateRuleResolver ruleResolver = new([easterSunday]);
        CachingCalculationAnchorResolver resolver = new([easterSunday], ruleResolver);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            resolver.Resolve(anchorRuleName!, 2024);
        });

        Assert.AreEqual("anchorRuleName", ex.ParamName);
    }

    /// <summary>
    /// Creates an algorithm-backed notable-date rule.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="algorithmType">The algorithm type.</param>
    /// <returns>The created rule.</returns>
    private static NotableDateRule AlgorithmRule(string name, Type algorithmType) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmType = algorithmType,
        };

    /// <summary>
    /// Test algorithm that returns a deterministic Easter-like date and records invocation count.
    /// </summary>
    private sealed class CountingAlgorithm
        : INotableDateAlgorithm
    {
        public static int CallCount { get; private set; }

        public static void Reset() => CallCount = 0;

        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
        {
            CallCount++;
            return new DateTime(year, 3, 31);
        }
    }

    /// <summary>
    /// Test algorithm that always returns <see langword="null" /> and records invocation count.
    /// </summary>
    private sealed class NullReturningAlgorithm
        : INotableDateAlgorithm
    {
        public static int CallCount { get; private set; }

        public static void Reset() => CallCount = 0;

        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
        {
            CallCount++;
            return null;
        }
    }

    /// <summary>
    /// Test algorithm that throws on the first invocation and succeeds on the second.
    /// </summary>
    private sealed class ThrowOnceAlgorithm
        : INotableDateAlgorithm
    {
        public static int CallCount { get; private set; }

        public static void Reset() => CallCount = 0;

        public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
        {
            CallCount++;

            if (CallCount == 1)
                throw new InvalidOperationException("Synthetic calculation failure.");

            return new DateTime(year, 3, 31);
        }
    }
}
