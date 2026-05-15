// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.TryDecode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodeTests
{
    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> returns
    /// <see langword="true" /> for a well-formed input, exposes the decoded value, and reports the number of
    /// bytes consumed.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenWellFormedInput_ShouldReturnTrueAndReportBytesConsumed()
    {
        bool result = Bencode.TryDecode(CanonicalIntegerBytes, out BencodedValue? value, out int consumed);

        Assert.IsTrue(result);
        Assert.IsNotNull(value);
        Assert.AreEqual(4, consumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> consumes
    /// only the prefix of well-formed bytes and ignores the trailing bytes (success path — the trailing-data
    /// rejection only applies to <see cref="Bencode.Decode(ReadOnlySpan{byte})" />).
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenWellFormedPrefixWithTrailingBytes_ShouldReturnTrueAndConsumeOnlyPrefix()
    {
        bool result = Bencode.TryDecode(Bytes("i3e0:trailing"), out BencodedValue? value, out int consumed);

        Assert.IsTrue(result);
        Assert.IsInstanceOfType<BencodedInteger>(value);
        Assert.AreEqual(3, consumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> returns
    /// <see langword="false" /> for a malformed input and leaves <c>value</c> at <see langword="null" /> and
    /// <c>bytesConsumed</c> at zero.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenMalformedInput_ShouldReturnFalseWithDefaults()
    {
        bool result = Bencode.TryDecode(Bytes("i03e"), out BencodedValue? value, out int consumed);

        Assert.IsFalse(result);
        Assert.IsNull(value);
        Assert.AreEqual(0, consumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> on empty
    /// input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenEmptyInput_ShouldReturnFalse()
    {
        bool result = Bencode.TryDecode(ReadOnlySpan<byte>.Empty, out BencodedValue? value, out int consumed);

        Assert.IsFalse(result);
        Assert.IsNull(value);
        Assert.AreEqual(0, consumed);
    }
}
