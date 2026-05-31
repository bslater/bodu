// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.Additional.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Aggregate
{

    /// <summary>
    /// Verifies that the 3-function with-index overload throws <see cref="ArgumentNullException" /> when any accumulator function is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenFuncIsNull_ForThreeFuncWithIndex_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, int> nullFunc = null!;

        ArgumentNullException ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                nullFunc,
                (a, x, i) => a + x,
                (a, x, i) => a + x);
        });
        Assert.AreEqual("func1", ex1.ParamName);

        ArgumentNullException ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x, i) => a + x,
                nullFunc,
                (a, x, i) => a + x);
        });
        Assert.AreEqual("func2", ex2.ParamName);

        ArgumentNullException ex3 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x, i) => a + x,
                (a, x, i) => a + x,
                nullFunc);
        });
        Assert.AreEqual("func3", ex3.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function no-index overload invokes its accumulator functions in declaration order for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncNoIndex_ShouldInvokeAccumulatorsInOrder()
    {
        int[] source = [1, 2, 3];
        var calls = new List<string>();

        _ = source.Aggregate(
            seed1: 0, seed2: 0, seed3: 0,
            func1: (acc, x) => { calls.Add($"f1:{x}"); return acc; },
            func2: (acc, x) => { calls.Add($"f2:{x}"); return acc; },
            func3: (acc, x) => { calls.Add($"f3:{x}"); return acc; });

        CollectionAssert.AreEqual(
            new[] { "f1:1", "f2:1", "f3:1", "f1:2", "f2:2", "f3:2", "f1:3", "f2:3", "f3:3" },
            calls);
    }

    /// <summary>
    /// Verifies that the 3-function with-index overload computes the correct aggregate values when invoked on a non-empty source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncWithIndex_ShouldReturnAllThreeFinalValues()
    {
        int[] source = [10, 20, 30, 40];

        // sumWithIndex = 10*0 + 20*1 + 30*2 + 40*3 = 0 + 20 + 60 + 120 = 200
        (var weightedSum, var count, var lastIndex) = source.Aggregate(
            seed1: 0,
            seed2: 0,
            seed3: -1,
            func1: (acc, x, i) => acc + (x * i),
            func2: (acc, _, _) => acc + 1,
            func3: (_, _, i) => i);

        Assert.AreEqual(200, weightedSum);
        Assert.AreEqual(4, count);
        Assert.AreEqual(3, lastIndex);
    }

    /// <summary>
    /// Verifies that the 3-function with-index with-selector overload combines index-aware accumulation with the result projection.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForThreeFuncWithIndexAndSelector_ShouldTransformTuple()
    {
        int[] source = [1, 2, 3];

        var result = source.Aggregate(
            seed1: 0,
            seed2: 0,
            seed3: 0,
            func1: (acc, x, i) => acc + x,
            func2: (acc, _, _) => acc + 1,
            func3: (acc, _, i) => acc + i,
            resultSelector: (sum, count, idxSum) => $"{sum}/{count}/{idxSum}");

        // sum = 6, count = 3, idxSum = 0 + 1 + 2 = 3
        Assert.AreEqual("6/3/3", result);
    }

    /// <summary>
    /// Verifies that the 2-function no-index overload invokes its accumulator functions in declaration order for each element.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenInvoked_ForTwoFuncNoIndex_ShouldInvokeAccumulatorsInOrder()
    {
        int[] source = [1, 2];
        var calls = new List<string>();

        _ = source.Aggregate(
            seed1: 0, seed2: 0,
            func1: (acc, x) => { calls.Add($"f1:{x}"); return acc; },
            func2: (acc, x) => { calls.Add($"f2:{x}"); return acc; });

        CollectionAssert.AreEqual(new[] { "f1:1", "f2:1", "f1:2", "f2:2" }, calls);
    }

    /// <summary>
    /// Verifies that the 3-function with-index with-selector overload throws <see cref="ArgumentNullException" /> when the result selector
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenResultSelectorIsNull_ForThreeFuncWithIndexAndSelector_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        Func<int, int, int, string> resultSelector = null!;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Aggregate(
                0, 0, 0,
                (a, x, i) => a + x,
                (a, x, i) => a + x,
                (a, x, i) => a + x,
                resultSelector);
        });

        Assert.AreEqual("resultSelector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the 3-function with-index overload returns the seeds unchanged when invoked on an empty source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForThreeFuncWithIndex_ShouldReturnSeedsUnchanged()
    {
        var source = Array.Empty<int>();

        (var v1, var v2, var v3) = source.Aggregate(
            seed1: 1, seed2: 2, seed3: 3,
            func1: (a, _, _) => a + 1,
            func2: (a, _, _) => a + 1,
            func3: (a, _, _) => a + 1);

        Assert.AreEqual(1, v1);
        Assert.AreEqual(2, v2);
        Assert.AreEqual(3, v3);
    }

    /// <summary>
    /// Verifies that the 3-function with-selector overload returns the projected result of the seeds when invoked on an empty source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForThreeFuncWithSelector_ShouldApplySelectorToSeeds()
    {
        var source = Array.Empty<int>();

        var result = source.Aggregate(
            seed1: 2, seed2: 3, seed3: 5,
            func1: (a, x) => a + x,
            func2: (a, x) => a + x,
            func3: (a, x) => a + x,
            resultSelector: (a, b, c) => $"{a},{b},{c}");

        Assert.AreEqual("2,3,5", result);
    }

    /// <summary>
    /// Verifies that the seeded with-index overload returns the seed unchanged and skips the accumulator when invoked on an empty source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForTwoFuncWithIndex_ShouldReturnSeedsUnchanged()
    {
        var source = Array.Empty<int>();

        (var v1, var v2) = source.Aggregate(
            seed1: 5,
            seed2: 9,
            func1: (a, _, _) => a + 1,
            func2: (a, _, _) => a + 1);

        Assert.AreEqual(5, v1);
        Assert.AreEqual(9, v2);
    }

    /// <summary>
    /// Verifies that the 2-function with-index with-selector overload returns the projected result of the seeds when invoked on an empty
    /// source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForTwoFuncWithIndexAndSelector_ShouldApplySelectorToSeeds()
    {
        var source = Array.Empty<int>();

        var result = source.Aggregate(
            seed1: 7,
            seed2: 11,
            func1: (a, x, i) => a + x,
            func2: (a, x, i) => a * x,
            resultSelector: (a, b) => $"{a}|{b}");

        Assert.AreEqual("7|11", result);
    }

    /// <summary>
    /// Verifies that the 2-function with-selector overload returns the projected result of the seeds when invoked on an empty source.
    /// </summary>
    [TestMethod]
    public void Aggregate_WhenSourceIsEmpty_ForTwoFuncWithSelector_ShouldApplySelectorToSeeds()
    {
        var source = Array.Empty<int>();

        var result = source.Aggregate(
            seed1: 7,
            seed2: 11,
            func1: (a, x) => a + x,
            func2: (a, x) => a * x,
            resultSelector: (a, b) => $"{a}|{b}");

        Assert.AreEqual("7|11", result);
    }

}
