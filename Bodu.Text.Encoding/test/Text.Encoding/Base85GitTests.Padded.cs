// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.Padded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that <see cref="Base85.EncodeGitPadded(ReadOnlySpan{byte})" /> always emits five characters per
    /// one-to-four-byte group.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedLength">The expected encoded character count.</param>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 5)]
    [DataRow(2, 5)]
    [DataRow(3, 5)]
    [DataRow(4, 5)]
    [DataRow(5, 10)]
    [DataRow(8, 10)]
    public void EncodeGitPadded_ShouldEmitFiveCharactersPerGroup(int byteCount, int expectedLength)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            bytes[i] = (byte)(i + 1);

        string encoded = Base85.EncodeGitPadded(bytes);

        Assert.AreEqual(expectedLength, encoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeGitPadded(ReadOnlySpan{byte})" /> matches the reference padded vectors.
    /// </summary>
    /// <param name="hex">The source bytes in hexadecimal.</param>
    /// <param name="expected">The expected padded Git Base85 text.</param>
    [TestMethod]
    [DataRow("01", "0RR91")]
    [DataRow("01020304", "0RjUA")]
    [DataRow("FFFFFFFF", "|NsC0")]
    public void EncodeGitPadded_ShouldMatchReferenceVectors(string hex, string expected)
    {
        byte[] bytes = Convert.FromHexString(hex);

        string encoded = Base85.EncodeGitPadded(bytes);

        Assert.AreEqual(expected, encoded);
    }

    /// <summary>
    /// Verifies that the padded "hello" vector encodes to its reference text.
    /// </summary>
    [TestMethod]
    public void EncodeGitPadded_WhenHello_ShouldMatchReference()
    {
        Assert.AreEqual("Xk~0{ZvX%Q", Base85.EncodeGitPadded(Ascii("hello")));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> trims the
    /// zero-padding bytes and returns exactly the supplied decoded length.
    /// </summary>
    /// <param name="hex">The original bytes in hexadecimal.</param>
    [TestMethod]
    [DataRow("01")]
    [DataRow("0102")]
    [DataRow("010203")]
    [DataRow("01020304")]
    [DataRow("68656C6C6F")]
    public void DecodeGitPadded_ShouldRecoverOriginalBytes(string hex)
    {
        byte[] original = Convert.FromHexString(hex);
        string encoded = Base85.EncodeGitPadded(original);

        byte[] decoded = Base85.DecodeGitPadded(encoded, original.Length);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeGitPadded(ReadOnlySpan{byte})" /> /
    /// <see cref="Base85.DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> round-trips across lengths 0
    /// through 256. Tagged Regression because of the length sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void DecodeGitPadded_ShouldRoundTripAcrossLengths()
    {
        var random = new Random(0x51AC);
        for (int length = 0; length <= 256; length++)
        {
            byte[] original = new byte[length];
            random.NextBytes(original);

            string encoded = Base85.EncodeGitPadded(original);
            byte[] decoded = Base85.DecodeGitPadded(encoded, length);

            CollectionAssert.AreEqual(original, decoded, $"Padded round trip failed for length={length}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentException" /> when the input length is not a multiple of five characters.
    /// </summary>
    [TestMethod]
    public void DecodeGitPadded_WhenLengthNotMultipleOfFive_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.DecodeGitPadded("0RjU".AsSpan(), 3);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the decoded length is negative.
    /// </summary>
    [TestMethod]
    public void DecodeGitPadded_WhenDecodedLengthNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.DecodeGitPadded("0RjUA".AsSpan(), -1);
        });

        Assert.AreEqual("decodedLength", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentException" /> when the decoded length is inconsistent with the number of encoded groups.
    /// </summary>
    /// <param name="decodedLength">The inconsistent decoded length.</param>
    [TestMethod]
    [DataRow(5)]
    [DataRow(8)]
    public void DecodeGitPadded_WhenDecodedLengthInconsistent_ShouldThrowArgumentException(int decodedLength)
    {
        // "0RjUA" is a single five-character group → at most four bytes.
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.DecodeGitPadded("0RjUA".AsSpan(), decodedLength);
        });

        Assert.AreEqual("decodedLength", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecodeGitPadded(ReadOnlySpan{char}, int, Span{byte}, out int, BaseFormatStyles)" />
    /// returns <see langword="false" /> when the framing is inconsistent, without throwing.
    /// </summary>
    [TestMethod]
    public void TryDecodeGitPadded_WhenFramingInconsistent_ShouldReturnFalse()
    {
        Span<byte> destination = new byte[16];

        bool success = Base85.TryDecodeGitPadded("0RjU".AsSpan(), 3, destination, out int bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncodeGitPadded(ReadOnlySpan{byte}, Span{char}, out int)" /> returns
    /// <see langword="false" /> and writes zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeGitPadded_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZero()
    {
        Span<char> destination = new char[4];

        bool success = Base85.TryEncodeGitPadded(Ascii("hello"), destination, out int charsWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetGitPaddedEncodedLength(int)" /> returns the padded character count for the
    /// supplied byte count.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expected">The expected padded character count.</param>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 5)]
    [DataRow(4, 5)]
    [DataRow(5, 10)]
    [DataRow(9, 15)]
    public void GetGitPaddedEncodedLength_ShouldReturnExpectedCount(int byteCount, int expected)
    {
        Assert.AreEqual(expected, Base85.GetGitPaddedEncodedLength(byteCount));
    }

    /// <summary>
    /// Verifies that the compact <see cref="Base85Variant.GitCompact" /> output and the padded Git binary-patch
    /// primitive are intentionally distinct for a partial final group: compact trims the tail (1 byte → 2 chars)
    /// while padded always emits five characters per group.
    /// </summary>
    [TestMethod]
    public void GitCompact_AndGitPadded_ShouldDiffer_ForPartialFinalGroup()
    {
        byte[] source = { 0x41 };

        string compact = Base85.Encode(source, Base85Variant.GitCompact);
        string padded = Base85.EncodeGitPadded(source);

        Assert.AreNotEqual(compact, padded);
        Assert.AreEqual(2, compact.Length);
        Assert.AreEqual(5, padded.Length);
    }
}
