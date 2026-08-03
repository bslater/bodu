// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeIdTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeIdTests
{
    /// <summary>
    /// Verifies the canonical eight-digit upper-case hexadecimal form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatted_ShouldUseEightHexDigits()
    {
        Assert.AreEqual("0x00000122", PstNodeId.RootFolder.ToString());
        Assert.AreEqual("0x00200024", new PstNodeId(0x00200024).ToString());
        Assert.AreEqual("0xFFFFFFFF", new PstNodeId(0xFFFFFFFF).ToString());
    }
}
