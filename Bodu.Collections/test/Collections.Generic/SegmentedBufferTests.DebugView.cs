// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class SegmentedBufferTests
{
    /// <summary>
    /// Verifies that <see cref="SegmentedBufferDebugView{T}.Items" /> exposes every buffered item in order,
    /// flattening across segment boundaries.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryItemInOrderAcrossSegments()
    {
        var sut = new SegmentedBuffer<int>(2);
        for (var i = 0; i < 5; i++)
        {
            sut.Add(i);
        }

        var view = new SegmentedBufferDebugView<int>(sut);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBufferDebugView{T}.Items" /> is empty for an empty buffer.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenBufferIsEmpty_ShouldBeEmpty()
    {
        var view = new SegmentedBufferDebugView<int>(new SegmentedBuffer<int>());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="SegmentedBufferDebugView{T}" /> with a <see langword="null" />
    /// buffer throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenBufferIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SegmentedBufferDebugView<int>(null!);
        });

        Assert.AreEqual("segmentedBuffer", ex.ParamName);
    }
}
