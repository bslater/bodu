// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.IsFinalBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

public partial class Utf8IniReaderTests
{
    /// <summary>
    /// Verifies that a reader built over a whole input, without a state, reads it as the final block.
    /// </summary>
    [TestMethod]
    public void IsFinalBlock_WhenBuiltOverAWholeInput_ShouldBeTrue()
    {
        var reader = new Utf8IniReader("k=v\n"u8);

        Assert.IsTrue(reader.IsFinalBlock);
    }

    /// <summary>
    /// Verifies that a reader built over a block reports whether the block was declared final.
    /// </summary>
    /// <param name="isFinalBlock">Whether the block is declared final.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsFinalBlock_WhenBuiltOverABlock_ShouldReportTheDeclaration(bool isFinalBlock)
    {
        var reader = new Utf8IniReader("k=v\n"u8, isFinalBlock, new IniReaderState());

        Assert.AreEqual(isFinalBlock, reader.IsFinalBlock);
    }
}
