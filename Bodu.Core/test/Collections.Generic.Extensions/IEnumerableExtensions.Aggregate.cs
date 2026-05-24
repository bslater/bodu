// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

using System;
using System.Collections.Generic;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Aggregate
{

    /// <summary>
    /// Verifies that the seeded overload allows the accumulator type to differ from the element type.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenAccumulatorDiffersFromElementType_ShouldReturnExpectedResult()
    {
        int[] source = [1, 2, 3];

        var result = source.Aggregate(
            "[",
            (acc, x, i) => acc + (i == 0 ? string.Empty : ",") + x);

        Assert.AreEqual("[1,2,3", result);
    }

    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="ArgumentNullException"/> when the accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForDefaultSeedOverload_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, int> func = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(func);
        });

        Assert.AreEqual("func", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the seeded overload throws <see cref="ArgumentNullException"/> when the accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForSeededOverload_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, int> func = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, func);
        });

        Assert.AreEqual("func", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload throws <see cref="ArgumentNullException"/> when any accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForThreeFuncNoIndex_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int> nullFunc = null!;

        ArgumentNullException ex3 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x) => a + x,
                (a, x) => a + x,
                nullFunc);
        });
        Assert.AreEqual("func3", ex3.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload throws <see cref="ArgumentNullException"/> when either accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForTwoFuncNoIndex_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int> nullFunc = null!;

        ArgumentNullException ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, nullFunc, (a, x) => a + x);
        });
        Assert.AreEqual("func1", ex1.ParamName);

        ArgumentNullException ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, nullFunc);
        });
        Assert.AreEqual("func2", ex2.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-index overload throws <see cref="ArgumentNullException"/> when either accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForTwoFuncWithIndex_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, int> nullFunc = null!;

        ArgumentNullException ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, nullFunc, (a, x, i) => a + x);
        });
        Assert.AreEqual("func1", ex1.ParamName);

        ArgumentNullException ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x, i) => a + x, nullFunc);
        });
        Assert.AreEqual("func2", ex2.ParamName);
    }

    /// <summary>
    /// Verifies that the default-seed overload applies the accumulator function starting from <see langword="default"/> and uses the
    /// first element as the first running value passed to <paramref name="func"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForDefaultSeedOverload_ShouldStartFromDefault()
    {
        int[] source = [5, 10, 20];

        var result = source.Aggregate((acc, x, i) => acc + x);

        // default(int) + 5 + 10 + 20 == 35
        Assert.AreEqual(35, result);
    }

    /// <summary>
    /// Verifies that both multi-accumulator overload families enumerate the source sequence exactly once.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForMultiAccumulator_ShouldEnumerateSourceOnce()
    {
        var enumerationCount2 = 0;
        IEnumerable<int> TwoFuncSource()
        {
            enumerationCount2++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        _ = TwoFuncSource().Aggregate(0, 0, (a, x) => a + x, (a, _) => a + 1);
        Assert.AreEqual(1, enumerationCount2);

        var enumerationCount3 = 0;
        IEnumerable<int> ThreeFuncSource()
        {
            enumerationCount3++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        _ = ThreeFuncSource().Aggregate(
            0, 0, 0,
            (a, x) => a + x,
            (a, _) => a + 1,
            (a, x) => Math.Max(a, x));
        Assert.AreEqual(1, enumerationCount3);
    }

    /// <summary>
    /// Verifies that the seeded overload applies the accumulator in order and incorporates the element index correctly.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForSeededOverload_ShouldApplyAccumulatorInOrderWithIndex()
    {
        int[] source = [1, 2, 3, 4];

        // 0 + (1*0) + (2*1) + (3*2) + (4*3) == 20
        var result = source.Aggregate(0, (acc, x, i) => acc + (x * i));

        Assert.AreEqual(20, result);
    }

    /// <summary>
    /// Verifies that the seeded overload enumerates the source sequence exactly once.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForSeededOverload_ShouldEnumerateSourceOnce()
    {
        var enumerationCount = 0;

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

    /// <summary>
    /// Verifies that the 3-function no-index overload returns all three final accumulator values in a single pass.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncNoIndex_ShouldReturnAllThreeFinalValues()
    {
        int[] source = [3, 1, 4, 1, 5, 9, 2, 6];

        (var sum, var count, var max) = source.Aggregate(
            seed1: 0,
            seed2: 0,
            seed3: int.MinValue,
            func1: (acc, x) => acc + x,
            func2: (acc, _) => acc + 1,
            func3: (acc, x) => Math.Max(acc, x));

        Assert.AreEqual(31, sum);
        Assert.AreEqual(8, count);
        Assert.AreEqual(9, max);
    }

    /// <summary>
    /// Verifies that the 3-function with-index overload passes the same monotonically increasing zero-based index to all three
    /// accumulator functions for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncWithIndex_ShouldShareMonotonicIndexAcrossFuncs()
    {
        int[] source = [5, 6, 7];
        var captured1 = new List<int>();
        var captured2 = new List<int>();
        var captured3 = new List<int>();

        _ = source.Aggregate(
            seed1: 0,
            seed2: 0,
            seed3: 0,
            func1: (acc, _, i) => { captured1.Add(i); return acc; },
            func2: (acc, _, i) => { captured2.Add(i); return acc; },
            func3: (acc, _, i) => { captured3.Add(i); return acc; });

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, captured1);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, captured2);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, captured3);
    }

    /// <summary>
    /// Verifies that the 3-function with-selector overload transforms the tuple result through the supplied selector.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncWithSelector_ShouldTransformTuple()
    {
        int[] source = [2, 4, 6, 8];

        // sum=20, count=4, product=2*4*6*8=384 → "20/4/384"
        var result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            seed3: 1,
            func1: (acc, x) => acc + x,
            func2: (acc, _) => acc + 1,
            func3: (acc, x) => acc * x,
            resultSelector: (sum, count, product) => $"{sum}/{count}/{product}");

        Assert.AreEqual("20/4/384", result);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload returns both final accumulator values in a single pass.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncNoIndex_ShouldReturnBothFinalValues()
    {
        int[] source = [3, 1, 4, 1, 5, 9, 2, 6];

        (var min, var max) = source.Aggregate(
            seed1: int.MaxValue,
            seed2: int.MinValue,
            func1: (acc, x) => Math.Min(acc, x),
            func2: (acc, x) => Math.Max(acc, x));

        Assert.AreEqual(1, min);
        Assert.AreEqual(9, max);
    }

    /// <summary>
    /// Verifies that the 2-function with-index overload passes the same monotonically increasing zero-based index to both accumulator
    /// functions for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncWithIndex_ShouldShareMonotonicIndexAcrossFuncs()
    {
        int[] source = [10, 20, 30, 40];
        var captured1 = new List<int>();
        var captured2 = new List<int>();

        _ = source.Aggregate(
            seed1: 0,
            seed2: 0,
            func1: (acc, x, i) => { captured1.Add(i); return acc + x; },
            func2: (acc, x, i) => { captured2.Add(i); return acc + x; });

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, captured1);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, captured2);
    }

    /// <summary>
    /// Verifies that the 2-function with-index with-selector overload combines index-aware accumulation with result projection.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncWithIndexAndSelector_ShouldTransformTuple()
    {
        int[] source = [2, 4, 6];

        // weighted sum: 2*0 + 4*1 + 6*2 = 16; count: 3
        var result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            func1: (acc, x, i) => acc + (x * i),
            func2: (acc, _, _) => acc + 1,
            resultSelector: (weighted, count) => $"{weighted}@{count}");

        Assert.AreEqual("16@3", result);
    }

    /// <summary>
    /// Verifies that the 2-function with-selector overload transforms the tuple result through the supplied selector.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncWithSelector_ShouldTransformTuple()
    {
        int[] source = [1, 2, 3, 4];

        var result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            func1: (acc, x) => acc + x,
            func2: (acc, _) => acc + 1,
            resultSelector: (sum, count) => $"{sum}/{count}");

        Assert.AreEqual("10/4", result);
    }

    /// <summary>
    /// Verifies that the accumulator function receives a monotonically increasing zero-based index for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ShouldPassMonotonicIndexStartingAtZero()
    {
        int[] source = [10, 20, 30, 40];
        var capturedIndices = new List<int>();

        _ = source.Aggregate(0, (acc, x, i) =>
        {
            capturedIndices.Add(i);
            return acc + x;
        });

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, capturedIndices);
    }

    /// <summary>
    /// Verifies that the selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForSelectorOverload_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, string> resultSelector = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, (acc, x, i) => acc + x, resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function with-selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForThreeFuncWithSelector_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, string> resultSelector = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x) => a + x,
                (a, x) => a + x,
                (a, x) => a + x,
                resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForTwoFuncWithSelector_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, string> resultSelector = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, (a, x) => a + x, resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the selector overload transforms the final accumulator value through the result selector.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorProvided_ShouldTransformFinalAccumulator()
    {
        int[] source = [2, 3, 5, 7];

        // sum = 17, selector doubles it -> 34
        var result = source.Aggregate(0, (acc, x, i) => acc + x, acc => acc * 2);

        Assert.AreEqual(34, result);
    }

    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="InvalidOperationException"/> when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForDefaultSeedOverload_ShouldThrowExactly()
    {
        int[] source = [];

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = source.Aggregate((acc, x, i) => acc + x);
        });

        Assert.AreEqual(ResourceStrings.Op_Invalid_EmptySequence, ex.Message);
    }

    /// <summary>
    /// Verifies that the seeded overload returns the seed unchanged when the source sequence is empty and does not invoke the
    /// accumulator.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForSeededOverload_ShouldReturnSeedUnchanged()
    {
        int[] source = [];
        var invocations = 0;

        var result = source.Aggregate(42, (acc, x, i) =>
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
        int[] source = [];

        var result = source.Aggregate(7, (acc, x, i) => acc + x, acc => $"value={acc}");

        Assert.AreEqual("value=7", result);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload returns the seeds unchanged when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForThreeFuncNoIndex_ShouldReturnSeedsUnchanged()
    {
        int[] source = [];

        (var v1, var v2, var v3) = source.Aggregate(
            11, 22, 33,
            (a, x) => a + x,
            (a, x) => a + x,
            (a, x) => a + x);

        Assert.AreEqual(11, v1);
        Assert.AreEqual(22, v2);
        Assert.AreEqual(33, v3);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload returns the seeds unchanged when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForTwoFuncNoIndex_ShouldReturnSeedsUnchanged()
    {
        int[] source = [];

        (var v1, var v2) = source.Aggregate(11, 22, (a, x) => a + x, (a, x) => a + x);

        Assert.AreEqual(11, v1);
        Assert.AreEqual(22, v2);
    }
    /// <summary>
    /// Verifies that the default-seed overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForDefaultSeedOverload_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate((acc, x, i) => acc + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the seeded overload throws <see cref="ArgumentNullException"/> when the source sequence is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForSeededOverload_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, (acc, x, i) => acc + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForThreeFuncNoIndex_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x) => a + x,
                (a, x) => a + x,
                (a, x) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function with-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForThreeFuncWithIndex_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x, i) => a + x,
                (a, x, i) => a + x,
                (a, x, i) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForTwoFuncNoIndex_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, (a, x) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForTwoFuncWithIndex_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x, i) => a + x, (a, x, i) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

}
