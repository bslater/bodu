// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.UnsignedIntegers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the unsigned 64-bit integer surface of <see cref="Utf8BencodeReader" />: integer tokens above
/// <see cref="long.MaxValue" /> are readable through <see cref="Utf8BencodeReader.GetUInt64" /> and
/// <see cref="Utf8BencodeReader.TryGetUInt64" /> while <see cref="Utf8BencodeReader.GetInt64" /> keeps rejecting them,
/// negative tokens are rejected by the unsigned accessors, and literals beyond <see cref="ulong.MaxValue" /> remain
/// malformed.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetUInt64" /> decodes non-negative integer tokens across the full
    /// unsigned 64-bit range, including values beyond <see cref="long.MaxValue" />.
    /// </summary>
    /// <param name="text">The Bencode integer token, expressed as Latin-1 text.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("i0e", 0UL)]
    [DataRow("i42e", 42UL)]
    [DataRow("i9223372036854775807e", 9223372036854775807UL)]
    [DataRow("i9223372036854775808e", 9223372036854775808UL)]
    [DataRow("i18446744073709551615e", ulong.MaxValue)]
    public void GetUInt64_WhenNonNegativeInteger_ShouldDecodeValue(string text, ulong expected)
    {
        var bytes = Bytes(text);
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(expected, reader.GetUInt64());
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetInt64" /> throws <see cref="BencodeFormatException" /> for an
    /// integer token whose value exceeds <see cref="long.MaxValue" />, preserving the signed accessor's range
    /// contract while <see cref="Utf8BencodeReader.Read" /> itself accepts the token.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenIntegerExceedsInt64Range_ShouldThrowBencodeFormatException()
    {
        var bytes = Bytes("i18446744073709551615e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetUInt64" /> throws <see cref="BencodeFormatException" /> for a
    /// negative integer token, which has no unsigned representation.
    /// </summary>
    [TestMethod]
    public void GetUInt64_WhenNegativeInteger_ShouldThrowBencodeFormatException()
    {
        var bytes = Bytes("i-1e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            Assert.IsTrue(reader.Read());
            _ = reader.GetUInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TryGetUInt64" /> returns <see langword="true" /> and the decoded
    /// value for a non-negative integer token beyond <see cref="long.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void TryGetUInt64_WhenNonNegativeInteger_ShouldReturnTrueAndValue()
    {
        var bytes = Bytes("i18446744073709551615e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.TryGetUInt64(out var value));
        Assert.AreEqual(ulong.MaxValue, value);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TryGetUInt64" /> returns <see langword="false" /> and a zero value
    /// for a negative integer token.
    /// </summary>
    [TestMethod]
    public void TryGetUInt64_WhenNegativeInteger_ShouldReturnFalse()
    {
        var bytes = Bytes("i-42e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.TryGetUInt64(out var value));
        Assert.AreEqual(0UL, value);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetUInt64" /> throws <see cref="InvalidOperationException" /> when
    /// the current token is not an integer.
    /// </summary>
    [TestMethod]
    public void GetUInt64_WhenTokenIsNotInteger_ShouldThrowInvalidOperationException()
    {
        var bytes = Bytes("4:spam");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            Assert.IsTrue(reader.Read());
            _ = reader.GetUInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TryGetUInt64" /> throws <see cref="InvalidOperationException" />
    /// when the current token is not an integer, mirroring the strict token contract of the other accessors.
    /// </summary>
    [TestMethod]
    public void TryGetUInt64_WhenTokenIsNotInteger_ShouldThrowInvalidOperationException()
    {
        var bytes = Bytes("le");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            Assert.IsTrue(reader.Read());
            _ = reader.TryGetUInt64(out _);
        });
    }

    /// <summary>
    /// Verifies that an integer literal one above <see cref="ulong.MaxValue" /> is rejected by
    /// <see cref="Utf8BencodeReader.Read" /> with <see cref="BencodeFormatException" />, pinning the upper bound of
    /// the widened integer surface.
    /// </summary>
    [TestMethod]
    public void Read_WhenIntegerExceedsUInt64Range_ShouldThrowBencodeFormatException()
    {
        var bytes = Bytes("i18446744073709551616e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            _ = reader.Read();
        });
    }
}
