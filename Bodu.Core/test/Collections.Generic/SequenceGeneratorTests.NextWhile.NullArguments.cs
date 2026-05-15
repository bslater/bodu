// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.NextWhile.NullArguments.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequenceGeneratorTests
{

    /// <summary>
    /// Verifies that the indexed-transform overload of <see cref="SequenceGenerator.NextWhile" /> throws
    /// <see cref="ArgumentNullException" /> when the condition handler is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenConditionHandlerIsNull_ForIndexedOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.NextWhile(0, (Func<int, bool>)null!, (x, _) => x + 1).ToArray();
        });
    }
    /// <summary>
    /// Verifies that the simple-transform overload of <see cref="SequenceGenerator.NextWhile" /> throws
    /// <see cref="ArgumentNullException" /> when the condition handler is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenConditionHandlerIsNull_ForSimpleOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.NextWhile(0, (Func<int, bool>)null!, x => x + 1).ToArray();
        });
    }

    /// <summary>
    /// Verifies that the state-based overload of <see cref="SequenceGenerator.NextWhile" /> throws
    /// <see cref="ArgumentNullException" /> when the condition handler is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenConditionHandlerIsNull_ForStateOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.NextWhile<int, int>(
                0, (Func<int, bool>)null!, x => x + 1, x => x).ToArray();
        });
    }

    /// <summary>
    /// Verifies that the indexed-transform overload receives a monotonically increasing zero-based index for each successor call.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenIndexedOverloadIsInvoked_ShouldPassMonotonicIndexStartingAtZero()
    {
        var capturedIndices = new List<int>();

        _ = SequenceGenerator.NextWhile(
            0,
            v => v < 5,
            (v, i) => { capturedIndices.Add(i); return v + 1; }).ToArray();

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, capturedIndices);
    }

    /// <summary>
    /// Verifies that the state-based overload returns an empty sequence when the predicate rejects the initial state.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenInitialConditionIsFalse_ForStateOverload_ShouldReturnEmptySequence()
    {
        var actual = SequenceGenerator.NextWhile(
            initialState: (Curr: 0, Step: 1),
            conditionHandler: s => false,
            iterateFunction: s => (s.Curr + s.Step, s.Step),
            resultSelector: s => s.Curr).ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that the simple-transform overload returns the seed when the predicate fails on its first evaluation against the seed.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenInitialConditionIsTrue_ForSimpleOverload_ShouldYieldSeedAsFirstElement()
    {
        var actual = SequenceGenerator.NextWhile(
            initialValue: 10,
            conditionHandler: x => x > 0,
            resultSelector: x => x - 5).ToArray();

        Assert.AreEqual(10, actual[0]);
    }

    /// <summary>
    /// Verifies that the indexed-transform overload of <see cref="SequenceGenerator.NextWhile" /> throws
    /// <see cref="ArgumentNullException" /> when the result selector is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenResultSelectorIsNull_ForIndexedOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.NextWhile(0, x => true, (Func<int, int, int>)null!).ToArray();
        });
    }

    /// <summary>
    /// Verifies that the state-based overload of <see cref="SequenceGenerator.NextWhile" /> throws
    /// <see cref="ArgumentNullException" /> when the result selector is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWhile_WhenResultSelectorIsNull_ForStateOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.NextWhile<int, int>(
                0, x => true, x => x + 1, (Func<int, int>)null!).ToArray();
        });
    }

}
