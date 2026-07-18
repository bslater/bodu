// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.IReadOnlyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}" /> implements
    /// <see cref="System.Collections.Generic.IReadOnlyCollection{T}" />.
    /// </summary>
    [TestMethod]
    public void IReadOnlyCollection_WhenTypeInspected_ShouldBeImplemented()
    {
        var buffer = new SegmentedBuffer<int>(4);

        Assert.IsInstanceOfType<IReadOnlyCollection<int>>(buffer);
    }

    /// <summary>
    /// Verifies that the <see cref="System.Collections.Generic.IReadOnlyCollection{T}.Count" /> member reports the
    /// number of buffered elements.
    /// </summary>
    [TestMethod]
    public void IReadOnlyCollection_Count_WhenBufferHasElements_ShouldReportElementCount()
    {
        var buffer = new SegmentedBuffer<int>(4);
        for (int i = 0; i < 7; i++)
            buffer.Add(i);

        IReadOnlyCollection<int> readOnly = buffer;

        Assert.AreEqual(7, readOnly.Count);
    }

    /// <summary>
    /// Verifies that enumerating through the <see cref="System.Collections.Generic.IReadOnlyCollection{T}" /> surface
    /// yields all elements in insertion order.
    /// </summary>
    [TestMethod]
    public void IReadOnlyCollection_WhenEnumerated_ShouldYieldInsertionOrderedElements()
    {
        var buffer = new SegmentedBuffer<int>(3);
        for (int i = 0; i < 8; i++)
            buffer.Add(i);

        IReadOnlyCollection<int> readOnly = buffer;

        CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), readOnly.ToArray());
    }
}
