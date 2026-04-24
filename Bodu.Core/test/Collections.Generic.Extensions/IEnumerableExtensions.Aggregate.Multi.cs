// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.Multi.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_AggregateMulti
{
    /// <summary>
    /// Verifies that the 2-function no-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForTwoFuncNoIndex_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, (a, x) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload throws <see cref="ArgumentNullException"/> when either accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForTwoFuncNoIndex_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int> nullFunc = null!;

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, nullFunc, (a, x) => a + x);
        });
        Assert.AreEqual("func1", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, nullFunc);
        });
        Assert.AreEqual("func2", ex2.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForTwoFuncWithIndex_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x, i) => a + x, (a, x, i) => a + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-index overload throws <see cref="ArgumentNullException"/> when either accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForTwoFuncWithIndex_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int, int> nullFunc = null!;

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, nullFunc, (a, x, i) => a + x);
        });
        Assert.AreEqual("func1", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x, i) => a + x, nullFunc);
        });
        Assert.AreEqual("func2", ex2.ParamName);
    }

    /// <summary>
    /// Verifies that the 2-function with-selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForTwoFuncWithSelector_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, string> resultSelector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(0, 0, (a, x) => a + x, (a, x) => a + x, resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForThreeFuncNoIndex_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
    /// Verifies that the 3-function no-index overload throws <see cref="ArgumentNullException"/> when any accumulator function is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForThreeFuncNoIndex_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int> nullFunc = null!;

        var ex3 = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
    /// Verifies that the 3-function with-index overload throws <see cref="ArgumentNullException"/> when the source sequence is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsNull_ForThreeFuncWithIndex_ShouldThrowArgumentNullException()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
    /// Verifies that the 3-function with-selector overload throws <see cref="ArgumentNullException"/> when the result selector is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForThreeFuncWithSelector_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        Func<int, int, int, string> resultSelector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
    /// Verifies that the 2-function no-index overload returns both final accumulator values in a single pass.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncNoIndex_ShouldReturnBothFinalValues()
    {
        int[] source = { 3, 1, 4, 1, 5, 9, 2, 6 };

        (int min, int max) = source.Aggregate(
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
        int[] source = { 10, 20, 30, 40 };
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
    /// Verifies that the 2-function with-selector overload transforms the tuple result through the supplied selector.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncWithSelector_ShouldTransformTuple()
    {
        int[] source = { 1, 2, 3, 4 };

        string result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            func1: (acc, x) => acc + x,
            func2: (acc, _) => acc + 1,
            resultSelector: (sum, count) => $"{sum}/{count}");

        Assert.AreEqual("10/4", result);
    }

    /// <summary>
    /// Verifies that the 2-function with-index with-selector overload combines index-aware accumulation with result projection.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncWithIndexAndSelector_ShouldTransformTuple()
    {
        int[] source = { 2, 4, 6 };

        // weighted sum: 2*0 + 4*1 + 6*2 = 16; count: 3
        string result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            func1: (acc, x, i) => acc + (x * i),
            func2: (acc, _, _) => acc + 1,
            resultSelector: (weighted, count) => $"{weighted}@{count}");

        Assert.AreEqual("16@3", result);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload returns the seeds unchanged when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForTwoFuncNoIndex_ShouldReturnSeedsUnchanged()
    {
        int[] source = Array.Empty<int>();

        (int v1, int v2) = source.Aggregate(11, 22, (a, x) => a + x, (a, x) => a + x);

        Assert.AreEqual(11, v1);
        Assert.AreEqual(22, v2);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload returns all three final accumulator values in a single pass.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncNoIndex_ShouldReturnAllThreeFinalValues()
    {
        int[] source = { 3, 1, 4, 1, 5, 9, 2, 6 };

        (int sum, int count, int max) = source.Aggregate(
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
        int[] source = { 5, 6, 7 };
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
        int[] source = { 2, 4, 6, 8 };

        // sum=20, count=4, product=2*4*6*8=384 → "20/4/384"
        string result = source.Aggregate(
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
    /// Verifies that the 3-function no-index overload returns the seeds unchanged when the source sequence is empty.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForThreeFuncNoIndex_ShouldReturnSeedsUnchanged()
    {
        int[] source = Array.Empty<int>();

        (int v1, int v2, int v3) = source.Aggregate(
            11, 22, 33,
            (a, x) => a + x,
            (a, x) => a + x,
            (a, x) => a + x);

        Assert.AreEqual(11, v1);
        Assert.AreEqual(22, v2);
        Assert.AreEqual(33, v3);
    }

    /// <summary>
    /// Verifies that both multi-accumulator overload families enumerate the source sequence exactly once.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForMultiAccumulator_ShouldEnumerateSourceOnce()
    {
        int enumerationCount2 = 0;
        IEnumerable<int> TwoFuncSource()
        {
            enumerationCount2++;
            yield return 1;
            yield return 2;
            yield return 3;
        }

        _ = TwoFuncSource().Aggregate(0, 0, (a, x) => a + x, (a, _) => a + 1);
        Assert.AreEqual(1, enumerationCount2);

        int enumerationCount3 = 0;
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
}
