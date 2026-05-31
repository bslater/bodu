// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Clear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Clear" /> on an already-empty set leaves it empty.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var sut = new RangeSet<int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that ranges added after <see cref="RangeSet{T}.Clear" /> are tracked normally.
    /// </summary>
    [TestMethod]
    public void Clear_WhenRangesReAddedAfterClear_ShouldTrackNewRanges()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        sut.Clear();
        sut.Add(0, 100);

        Assert.AreEqual(1, sut.Count);
        AssertContents(sut, (0, 100));
    }
    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Clear" /> empties a populated set.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetHasRanges_ShouldRemoveAllRanges()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains(0));
        Assert.IsFalse(sut.Contains(12));
    }

}
