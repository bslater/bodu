// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Aggregate
{
    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForDefaultSeedOverload_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate((acc, x, i) => acc + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="ArgumentNullException"/> when the accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForDefaultSeedOverload_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int, int> func = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(func);
        });

        Assert.AreEqual("func", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the seeded overload throws <see cref="ArgumentNullException"/> when the source sequence is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForSeededOverload_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, (acc, x, i) => acc + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the seeded overload throws <see cref="ArgumentNullException"/> when the accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForSeededOverload_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int, int> func = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, func);
        });

        Assert.AreEqual("func", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForSelectorOverload_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, string> resultSelector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, (acc, x, i) => acc + x, resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="InvalidOperationException"/> when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForDefaultSeedOverload_ShouldThrowInvalidOperationException()
    {
        int[] source = Array.Empty<int>();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = source.Aggregate((acc, x, i) => acc + x);
        });

        Assert.AreEqual(ResourceStrings.InvalidOperation_EmptySequence, ex.Message);
    }

    /// <summary>
    /// Verifies that the seeded overload returns the seed unchanged when the source sequence is empty and does not invoke the
    /// accumulator.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForSeededOverload_ShouldReturnSeedUnchanged()
    {
        int[] source = Array.Empty<int>();
        int invocations = 0;

        int result = source.Aggregate(42, (acc, x, i) =>
        {
            invocations++;
            return acc + x;
        });

        Assert.AreEqual(42, result);
        Assert.AreEqual(0, invocations);
    }

    /// <summary>
    /// Verifies that the selector overload applies the result selector to the seed when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForSelectorOverload_ShouldApplySelectorToSeed()
    {
        int[] source = Array.Empty<int>();

        string result = source.Aggregate(7, (acc, x, i) => acc + x, acc => $"value={acc}");

        Assert.AreEqual("value=7", result);
    }

    /// <summary>
    /// Verifies that the accumulator function receives a monotonically increasing zero-based index for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ShouldPassMonotonicIndexStartingAtZero()
    {
        int[] source = { 10, 20, 30, 40 };
        var capturedIndices = new List<int>();

        _ = source.Aggregate(0, (acc, x, i) =>
        {
            capturedIndices.Add(i);
            return acc + x;
        });

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, capturedIndices);
    }

    /// <summary>
    /// Verifies that the default-seed overload applies the accumulator function starting from <see langword="default"/> and uses the
    /// first element as the first running value passed to <paramref name="func"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForDefaultSeedOverload_ShouldStartFromDefault()
    {
        int[] source = { 5, 10, 20 };

        int result = source.Aggregate((acc, x, i) => acc + x);

        // default(int) + 5 + 10 + 20 == 35
        Assert.AreEqual(35, result);
    }

    /// <summary>
    /// Verifies that the seeded overload applies the accumulator in order and incorporates the element index correctly.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForSeededOverload_ShouldApplyAccumulatorInOrderWithIndex()
    {
        int[] source = { 1, 2, 3, 4 };

        // 0 + (1*0) + (2*1) + (3*2) + (4*3) == 20
        int result = source.Aggregate(0, (acc, x, i) => acc + (x * i));

        Assert.AreEqual(20, result);
    }

    /// <summary>
    /// Verifies that the seeded overload allows the accumulator type to differ from the element type.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenAccumulatorDiffersFromElementType_ShouldReturnExpectedResult()
    {
        int[] source = { 1, 2, 3 };

        string result = source.Aggregate(
            "[",
            (acc, x, i) => acc + (i == 0 ? string.Empty : ",") + x);

        Assert.AreEqual("[1,2,3", result);
    }

    /// <summary>
    /// Verifies that the selector overload transforms the final accumulator value through the result selector.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorProvided_ShouldTransformFinalAccumulator()
    {
        int[] source = { 2, 3, 5, 7 };

        // sum = 17, selector doubles it -> 34
        int result = source.Aggregate(0, (acc, x, i) => acc + x, acc => acc * 2);

        Assert.AreEqual(34, result);
    }

    /// <summary>
    /// Verifies that the seeded overload enumerates the source sequence exactly once.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForSeededOverload_ShouldEnumerateSourceOnce()
    {
        int enumerationCount = 0;

        IEnumerable<int> Source()
        {
            enumerationCount++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        _ = Source().Aggregate(0, (acc, x, i) => acc + x);

        Assert.AreEqual(1, enumerationCount);
    }
}
