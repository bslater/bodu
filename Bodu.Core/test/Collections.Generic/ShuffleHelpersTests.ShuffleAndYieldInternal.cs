// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.ShuffleAndYieldInternal.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ShuffleHelpersTests
{

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYieldInternal{T}(T[], IRandomGenerator, int)" /> yields every element of
    /// the supplied buffer exactly once when <c>count</c> equals the buffer length, exercising the iterator's loop body, and that
    /// it terminates correctly when the count counter reaches zero.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYieldInternal_WhenCountEqualsLength_ShouldYieldAllElementsExactlyOnce()
    {
        var buffer = new[] { 1, 2, 3, 4, 5 };
        var actual = ShuffleHelpers.ShuffleAndYieldInternal(buffer, new XorShiftRandom(7), buffer.Length).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYieldInternal{T}(T[], IRandomGenerator, int)" /> yields a partial subset
    /// when <c>count</c> is less than the buffer length, exercising both the loop body and the early termination path.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYieldInternal_WhenCountIsLessThanLength_ShouldYieldRequestedSubset()
    {
        var buffer = new[] { 10, 20, 30, 40, 50 };

        var actual = ShuffleHelpers.ShuffleAndYieldInternal(buffer, new XorShiftRandom(13), 3).ToArray();

        Assert.HasCount(3, actual);
        CollectionAssert.AllItemsAreUnique(actual);
        foreach (var value in actual)
            Assert.IsTrue(value is 10 or 20 or 30 or 40 or 50);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYieldInternal{T}(T[], IRandomGenerator, int)" /> yields nothing when the
    /// requested <c>count</c> is zero, exercising the loop's immediate-exit branch.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYieldInternal_WhenCountIsZero_ShouldYieldNoElements()
    {
        var buffer = new[] { 1, 2, 3 };

        var actual = ShuffleHelpers.ShuffleAndYieldInternal(buffer, new XorShiftRandom(7), 0).ToArray();

        Assert.IsEmpty(actual);
    }

}
