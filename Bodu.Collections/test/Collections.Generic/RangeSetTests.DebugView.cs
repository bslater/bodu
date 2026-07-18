// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{
    /// <summary>
    /// Verifies that <see cref="RangeSetDebugView{T}.Items" /> exposes every stored range.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryRange()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        var view = new RangeSetDebugView<int>(sut);

        CollectionAssert.AreEqual(sut.ToArray(), view.Items);
        Assert.AreEqual(2, view.Items.Length);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSetDebugView{T}.Items" /> is empty for an empty set.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenSetIsEmpty_ShouldBeEmpty()
    {
        var view = new RangeSetDebugView<int>(new RangeSet<int>());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> set.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenSetIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new RangeSetDebugView<int>(null!);
        });

        Assert.AreEqual("rangeSet", ex.ParamName);
    }
}
