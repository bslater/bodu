// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>
    /// Verifies that <see cref="BitSet.ToString" /> reports the documented summary form for an empty set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetIsEmpty_ShouldReportZeroCardinalityAndLength()
    {
        var bits = new BitSet();

        Assert.AreEqual("BitSet(Cardinality = 0, Length = 0)", bits.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.ToString" /> reports the current cardinality and logical length.
    /// </summary>
    [TestMethod]
    public void ToString_WhenBitsSet_ShouldReportCardinalityAndLength()
    {
        var bits = CreateWithBits(1, 3, 64);

        Assert.AreEqual("BitSet(Cardinality = 3, Length = 65)", bits.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BitSet.ToString" /> reflects mutations — the summary tracks the logical content, not
    /// the allocation.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetMutated_ShouldTrackLogicalContent()
    {
        var bits = CreateWithBits(200);

        bits.Clear(200);

        Assert.AreEqual("BitSet(Cardinality = 0, Length = 0)", bits.ToString());
    }
}
