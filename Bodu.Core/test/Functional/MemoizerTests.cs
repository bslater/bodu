// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemoizerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

/// <summary>
/// Contains unit tests for the <see cref="Memoizer" /> helper.
/// </summary>
[TestClass]
public sealed class MemoizerTests
{
    /// <summary>
    /// Verifies that a memoized function returns the computed result and reuses it on repeat calls.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Memoize_WhenCalledRepeatedly_ShouldComputeOncePerArgument()
    {
        var invocations = 0;
        var memoized = Memoizer.Memoize<int, int>(x =>
        {
            invocations++;
            return x * x;
        });

        Assert.AreEqual(25, memoized(5));
        Assert.AreEqual(25, memoized(5));
        Assert.AreEqual(9, memoized(3));

        Assert.AreEqual(2, invocations);
    }

    /// <summary>
    /// Verifies that a throwing function is not cached, so a later call retries.
    /// </summary>
    [TestMethod]
    public void Memoize_WhenFunctionThrows_ShouldNotCacheAndShouldRetry()
    {
        var attempts = 0;
        var memoized = Memoizer.Memoize<int, int>(x =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("transient");

            return x;
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => memoized(1));
        Assert.AreEqual(1, memoized(1));
        Assert.AreEqual(2, attempts);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> result is cached like any other value.
    /// </summary>
    [TestMethod]
    public void Memoize_WhenResultIsNull_ShouldCacheNull()
    {
        var invocations = 0;
        var memoized = Memoizer.Memoize<int, string?>(_ =>
        {
            invocations++;
            return null;
        });

        Assert.IsNull(memoized(1));
        Assert.IsNull(memoized(1));
        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that a supplied comparer governs argument identity.
    /// </summary>
    [TestMethod]
    public void Memoize_WhenComparerSupplied_ShouldTreatEquivalentArgumentsAsOne()
    {
        var invocations = 0;
        var memoized = Memoizer.Memoize<string, int>(
            s =>
            {
                invocations++;
                return s.Length;
            },
            StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(5, memoized("Hello"));
        Assert.AreEqual(5, memoized("HELLO"));
        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that the two-argument overload caches on the pair of arguments.
    /// </summary>
    [TestMethod]
    public void Memoize_WhenTwoArguments_ShouldCacheOnArgumentPair()
    {
        var invocations = 0;
        var memoized = Memoizer.Memoize<int, int, int>((a, b) =>
        {
            invocations++;
            return a + b;
        });

        Assert.AreEqual(7, memoized(3, 4));
        Assert.AreEqual(7, memoized(3, 4));
        Assert.AreEqual(5, memoized(2, 3));

        Assert.AreEqual(2, invocations);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> function throws <see cref="ArgumentNullException" /> naming <c>func</c>.
    /// </summary>
    [TestMethod]
    public void Memoize_WhenFunctionIsNull_ShouldThrowForFunc()
    {
        Assert.AreEqual(
            "func",
            Assert.ThrowsExactly<ArgumentNullException>(() => Memoizer.Memoize<int, int>(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that concurrent callers all observe the correct result without error.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Memoize_WhenCalledConcurrently_ShouldReturnCorrectResults()
    {
        var memoized = Memoizer.Memoize<int, int>(x => x * 2);

        var results = new int[1000];
        Parallel.For(0, results.Length, i =>
        {
            results[i] = memoized(i % 10);
        });

        for (var i = 0; i < results.Length; i++)
            Assert.AreEqual((i % 10) * 2, results[i]);
    }
}
