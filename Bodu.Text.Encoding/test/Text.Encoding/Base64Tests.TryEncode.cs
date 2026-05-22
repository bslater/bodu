// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.TryEncode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> succeeds when the destination is exactly the required size for
    /// representative inputs.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedCharCount">The expected output char count (Standard variant, padded).</param>
    [TestMethod]
    [DataRow(1, 4)]
    [DataRow(2, 4)]
    [DataRow(3, 4)]
    [DataRow(4, 8)]
    [DataRow(5, 8)]
    [DataRow(6, 8)]
    [DataRow(9, 12)]
    public void TryEncode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(int byteCount, int expectedCharCount)
    {
        var bytes = new byte[byteCount];
        var destination = new char[expectedCharCount];

        var ok = Base64.TryEncode(bytes.AsSpan(), destination, out var charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedCharCount, charsWritten);
    }
    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> succeeds when the destination is large enough.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationLargeEnough_ShouldReturnTrueAndExactCharCount()
    {
        var bytes = Ascii("foobar");
        var destination = new char[8];

        var ok = Base64.TryEncode(bytes.AsSpan(), destination, out var charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual("Zm9vYmFy", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> returns <see langword="false" /> when the destination is exactly
    /// one character short.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="exactRequired">The exact required char count.</param>
    [TestMethod]
    [DataRow(1, 4)]
    [DataRow(3, 4)]
    [DataRow(6, 8)]
    public void TryEncode_WhenDestinationOneCharShort_ShouldReturnFalseAndZeroCharsWritten(int byteCount, int exactRequired)
    {
        var bytes = new byte[byteCount];
        var destination = new char[exactRequired - 1];

        var ok = Base64.TryEncode(bytes.AsSpan(), destination, out var charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> returns <see langword="false" /> when the destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroCharsWritten()
    {
        var bytes = Ascii("foobar");
        var destination = new char[1];

        var ok = Base64.TryEncode(bytes.AsSpan(), destination, out var charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> on empty input returns <see langword="true" /> with
    /// <c>charsWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenInputIsEmpty_ShouldReturnTrueAndZeroCharsWritten()
    {
        var destination = new char[4];

        var ok = Base64.TryEncode(ReadOnlySpan<byte>.Empty, destination, out var charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> honours <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenOmitPadding_ShouldWriteFewerCharacters()
    {
        var bytes = Ascii("foo");
        var destination = new char[4];

        var ok = Base64.TryEncode(bytes.AsSpan(), destination, out var charsWritten, Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, charsWritten);
        Assert.AreEqual("Zm9v", new string(destination, 0, charsWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> rejects an undefined variant.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenUndefinedVariant_ShouldThrowExactly()
    {
        var destination = new char[16];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base64.TryEncode(Ascii("foo").AsSpan(), destination, out _, (Base64Variant)99);
        });
    }

}
