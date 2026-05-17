// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.Range.Overflow.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequenceGeneratorTests
{

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Range(int, int, int)" /> terminates cleanly when ascending iteration would overflow
    /// <see cref="int.MaxValue" />, yielding only the values that fit within the addressable range.
    /// </summary>
    [TestMethod]
    public void Range_WhenAscendingOverflowOccursDuringIteration_ShouldTerminateCleanly()
    {
        var actual = SequenceGenerator.Range(int.MaxValue - 1, int.MaxValue, 5).ToArray();

        // Start emits int.MaxValue - 1, then int.MaxValue - 1 + 5 overflows so iteration ends.
        CollectionAssert.AreEqual(new[] { int.MaxValue - 1 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Range(int, int, int)" /> terminates cleanly when descending iteration would underflow
    /// <see cref="int.MinValue" />, yielding only the values that fit within the addressable range.
    /// </summary>
    [TestMethod]
    public void Range_WhenDescendingOverflowOccursDuringIteration_ShouldTerminateCleanly()
    {
        var actual = SequenceGenerator.Range(int.MinValue + 1, int.MinValue, -5).ToArray();

        // Start emits int.MinValue + 1, then int.MinValue + 1 - 5 underflows so iteration ends.
        CollectionAssert.AreEqual(new[] { int.MinValue + 1 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Range(long, int)" /> emitted at <see cref="long.MaxValue" /> with count zero produces an empty sequence.
    /// </summary>
    [TestMethod]
    public void Range_WhenLongStartIsMaxAndCountIsZero_ShouldReturnEmptySequence()
    {
        var actual = SequenceGenerator.Range(long.MaxValue, 0).ToArray();
        Assert.IsEmpty(actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Range(int, int, int)" /> with a positive step but a stop endpoint preceding the start returns
    /// an empty sequence.
    /// </summary>
    [TestMethod]
    public void Range_WhenPositiveStepButStopBelowStart_ShouldReturnEmptySequence()
    {
        var actual = SequenceGenerator.Range(10, 5, 1).ToArray();
        Assert.IsEmpty(actual);
    }

}
