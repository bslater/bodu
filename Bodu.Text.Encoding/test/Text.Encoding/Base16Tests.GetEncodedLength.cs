// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.GetEncodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength" /> returns twice the byte count for default options.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenDefaultOptions_ShouldReturnTwiceByteCount() => Assert.AreEqual(8, Base16.GetEncodedLength(4));

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength" /> reports two characters per byte plus
    /// <c>byteCount - 1</c> separators when <see cref="BaseFormattingOptions.InsertSpacing" /> is set.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenInsertSpacing_ShouldReportTwoPerByteMinusOneSeparator() => Assert.AreEqual(11, Base16.GetEncodedLength(4, BaseFormattingOptions.InsertSpacing));

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength" /> rejects a negative byte count.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenNegativeByteCount_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.GetEncodedLength(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength" /> returns the prefix length when the byte count is zero and
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> is set.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenZeroBytesAndIncludePrefix_ShouldReturnPrefixLength() => Assert.AreEqual(2, Base16.GetEncodedLength(0, BaseFormattingOptions.IncludePrefix));

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength" /> returns zero for an empty input with default options.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenZeroBytesAndNoPrefix_ShouldReturnZero() => Assert.AreEqual(0, Base16.GetEncodedLength(0));

}
