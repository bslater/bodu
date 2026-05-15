// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.TryEncode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> succeeds when the destination is large enough.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationLargeEnough_ShouldReturnTrueAndExactCharCount()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[16];

        bool ok = Base32.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(16, charsWritten);
        Assert.AreEqual("MZXW6YTBOI======", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> returns <see langword="false" /> when the destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroCharsWritten()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[1];

        bool ok = Base32.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> for empty input returns <see langword="true" /> with
    /// <c>charsWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenInputIsEmpty_ShouldReturnTrueAndZeroCharsWritten()
    {
        char[] destination = new char[4];

        bool ok = Base32.TryEncode(ReadOnlySpan<byte>.Empty, destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> honours <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenOmitPadding_ShouldWriteFewerCharacters()
    {
        byte[] bytes = Ascii("foo");
        char[] destination = new char[8];

        bool ok = Base32.TryEncode(bytes.AsSpan(), destination, out int charsWritten, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(5, charsWritten);
        Assert.AreEqual("MZXW6", new string(destination, 0, charsWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> succeeds when the destination is exactly the required size for
    /// representative input lengths.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedCharCount">The expected output character count (Standard variant, default padding).</param>
    [TestMethod]
    [DataRow(1, 8)]
    [DataRow(2, 8)]
    [DataRow(3, 8)]
    [DataRow(4, 8)]
    [DataRow(5, 8)]
    [DataRow(6, 16)]
    [DataRow(10, 16)]
    [DataRow(11, 24)]
    public void TryEncode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(int byteCount, int expectedCharCount)
    {
        byte[] bytes = new byte[byteCount];
        char[] destination = new char[expectedCharCount];

        bool ok = Base32.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedCharCount, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> returns <see langword="false" /> when the destination is exactly
    /// one character less than required.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="exactRequired">The exact required char count.</param>
    [TestMethod]
    [DataRow(1, 8)]
    [DataRow(5, 8)]
    [DataRow(6, 16)]
    public void TryEncode_WhenDestinationOneCharShort_ShouldReturnFalseAndZeroCharsWritten(int byteCount, int exactRequired)
    {
        byte[] bytes = new byte[byteCount];
        char[] destination = new char[exactRequired - 1];

        bool ok = Base32.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> rejects an undefined variant with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        char[] destination = new char[32];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.TryEncode(Ascii("foo").AsSpan(), destination, out _, (Base32Variant)99);
        });
    }
}
