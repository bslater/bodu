// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeIdTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeIdTests
{
    /// <summary>
    /// Verifies that the largest 27-bit index composes and round-trips through the type and index accessors.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIndexIsMaximum_ShouldRoundTrip()
    {
        var id = new PstNodeId(PstNodeType.NormalFolder, 0x07FF_FFFF);

        Assert.AreEqual(PstNodeType.NormalFolder, id.Type);
        Assert.AreEqual(0x07FF_FFFFu, id.Index);
    }

    /// <summary>
    /// Verifies that an index above the 27-bit space is rejected instead of being silently truncated onto another
    /// identifier.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIndexExceeds27Bits_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PstNodeId(PstNodeType.NormalFolder, 0x0800_0000);
        });
    }
}
