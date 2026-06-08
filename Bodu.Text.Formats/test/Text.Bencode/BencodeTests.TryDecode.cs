// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.TryParse(ReadOnlySpan{byte}, out BencodedValue, out int)" /> on empty
    /// input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenEmptyInput_ShouldReturnFalse()
    {
        var result = Bencode.TryParse(ReadOnlySpan<byte>.Empty, out BencodedValue? value, out var consumed);

        Assert.IsFalse(result);
        Assert.IsNull(value);
        Assert.AreEqual(0, consumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryParse(ReadOnlySpan{byte}, out BencodedValue, out int)" /> returns
    /// <see langword="false" /> for a malformed input and leaves <c>value</c> at <see langword="null" /> and
    /// <c>bytesConsumed</c> at zero.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenMalformedInput_ShouldReturnFalseWithDefaults()
    {
        var result = Bencode.TryParse(Bytes("i03e"), out BencodedValue? value, out var consumed);

        Assert.IsFalse(result);
        Assert.IsNull(value);
        Assert.AreEqual(0, consumed);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.TryParse(ReadOnlySpan{byte}, out BencodedValue, out int)" /> returns
    /// <see langword="true" /> for a well-formed input, exposes the decoded value, and reports the number of
    /// bytes consumed.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenWellFormedInput_ShouldReturnTrueAndReportBytesConsumed()
    {
        var result = Bencode.TryParse(CanonicalIntegerBytes, out BencodedValue? value, out var consumed);

        Assert.IsTrue(result);
        Assert.IsNotNull(value);
        Assert.AreEqual(4, consumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryParse(ReadOnlySpan{byte}, out BencodedValue, out int)" /> consumes
    /// only the prefix of well-formed bytes and ignores the trailing bytes (success path — the trailing-data
    /// rejection only applies to <see cref="Bencode.Parse(ReadOnlySpan{byte})" />).
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenWellFormedPrefixWithTrailingBytes_ShouldReturnTrueAndConsumeOnlyPrefix()
    {
        var result = Bencode.TryParse(Bytes("i3e0:trailing"), out BencodedValue? value, out var consumed);

        Assert.IsTrue(result);
        Assert.IsInstanceOfType<BencodedInteger>(value);
        Assert.AreEqual(3, consumed);
    }

}
