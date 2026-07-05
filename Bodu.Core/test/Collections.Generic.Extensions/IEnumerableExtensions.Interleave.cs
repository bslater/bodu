// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Interleave.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Interleave
{
    /// <summary>
    /// Verifies that <c>Interleave</c> rotates strictly round-robin across inputs of equal length.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenInputsHaveEqualLength_ShouldRotateRoundRobin()
    {
        int[] result = new[] { 1, 2, 3 }.Interleave(new[] { 10, 20, 30 }).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 10, 2, 20, 3, 30 }, result);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> skips exhausted inputs and continues rotating over the surviving ones when the
    /// inputs have unequal lengths.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenInputsHaveUnequalLengths_ShouldSkipExhaustedAndContinue()
    {
        int[] result = new[] { 1, 2, 3 }.Interleave(new[] { 10, 20 }, new[] { 100 }).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 10, 100, 2, 20, 3 }, result);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> with no additional sequences yields the first sequence unchanged.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenOthersIsEmpty_ShouldReturnFirstSequence()
    {
        int[] result = new[] { 1, 2, 3 }.Interleave().ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
    }

    /// <summary>
    /// Verifies that an empty first sequence contributes nothing and the remaining inputs continue round-robin.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenFirstIsEmpty_ShouldContinueWithOthers()
    {
        int[] result = Array.Empty<int>().Interleave(new[] { 10, 20 }, new[] { 100, 200 }).ToArray();

        CollectionAssert.AreEqual(new[] { 10, 100, 20, 200 }, result);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> returns an empty sequence when every input is empty.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenAllInputsAreEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().Interleave(Array.Empty<int>(), Array.Empty<int>()));

    /// <summary>
    /// Verifies that <c>Interleave</c> defers enumeration of the inputs until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenResultNotEnumerated_ShouldNotEnumerateInputs()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Interleave(Source());

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> enumerates each input exactly once and disposes every enumerator, including
    /// when the inputs are of unequal length.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenEnumerated_ShouldEnumerateEachInputOnceAndDisposeAll()
    {
        var first = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });
        var second = new DisposeTrackingEnumerable<int>(new[] { 10 });

        _ = first.Interleave(second).ToArray();

        Assert.AreEqual(1, first.GetEnumeratorCallCount);
        Assert.AreEqual(1, first.DisposeCallCount);
        Assert.AreEqual(1, second.GetEnumeratorCallCount);
        Assert.AreEqual(1, second.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> disposes every enumerator when the result is abandoned before all inputs are
    /// exhausted.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenResultAbandonedEarly_ShouldDisposeAllEnumerators()
    {
        var first = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });
        var second = new DisposeTrackingEnumerable<int>(new[] { 10, 20, 30 });

        _ = first.Interleave(second).Take(2).ToArray();

        Assert.AreEqual(1, first.DisposeCallCount);
        Assert.AreEqual(1, second.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> throws <see cref="ArgumentNullException" /> when the first sequence is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenFirstIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> first = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = first.Interleave(new[] { 1 });
        });

        Assert.AreEqual("first", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Interleave</c> throws <see cref="ArgumentNullException" /> when the additional-sequences array
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenOthersIsNull_ShouldThrowExactly()
    {
        IEnumerable<int>[] others = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1 }.Interleave(others);
        });

        Assert.AreEqual("others", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> element in the additional-sequences array throws
    /// <see cref="ArgumentException" /> eagerly at the call site, before the result is iterated.
    /// </summary>
    [TestMethod]
    public void Interleave_WhenOthersContainsNullElement_ShouldThrowExactlyOnCall()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new[] { 1 }.Interleave(new[] { 2 }, null!);
        });

        Assert.AreEqual("others", ex.ParamName);
    }
}
