// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.TryEncode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, BaseFormattingOptions)" />
    /// returns <see langword="true" /> and writes the expected character count for a sufficient destination.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationLargeEnough_ShouldReturnTrueAndExactCharCount()
    {
        char[] destination = new char[CanonicalBytes.Length * 2];

        bool ok = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual(CanonicalHexLower, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, BaseFormattingOptions)" />
    /// returns <see langword="false" /> with <c>charsWritten = 0</c> when the destination is undersized.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroCharsWritten()
    {
        char[] destination = new char[1];

        bool ok = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out int charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, BaseFormattingOptions)" />
    /// returns <see langword="true" /> with <c>charsWritten = 0</c> for empty input.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenInputIsEmpty_ShouldReturnTrueAndZeroCharsWritten()
    {
        char[] destination = new char[4];

        bool ok = Base16.TryEncode(ReadOnlySpan<byte>.Empty, destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, BaseFormattingOptions)" />
    /// respects <see cref="BaseFormattingOptions.UpperCase" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenUpperCaseFlag_ShouldWriteUpperCaseDigits()
    {
        char[] destination = new char[CanonicalBytes.Length * 2];

        bool ok = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out _, BaseFormattingOptions.UpperCase);

        Assert.IsTrue(ok);
        Assert.AreEqual(CanonicalHexUpper, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, BaseFormattingOptions)" />
    /// rejects unsupported formatting flags with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenUnsupportedFlagsRequested_ShouldThrowArgumentException()
    {
        char[] destination = new char[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out _, BaseFormattingOptions.IncludePrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode" /> succeeds when the destination is exactly the required size.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(7)]
    [DataRow(64)]
    [DataRow(1024)]
    public void TryEncode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = (byte)i;
        }

        char[] destination = new char[byteCount * 2];

        bool ok = Base16.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(byteCount * 2, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode" /> returns <see langword="false" /> when the destination is exactly
    /// one character short of the required size.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(7)]
    [DataRow(64)]
    public void TryEncode_WhenDestinationOneCharShort_ShouldReturnFalseAndZeroCharsWritten(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        char[] destination = new char[(byteCount * 2) - 1];

        bool ok = Base16.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode" /> with a destination significantly larger than required still
    /// returns the exact char count and writes only into the leading prefix of the destination.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationOversized_ShouldReturnExactCharCount()
    {
        byte[] bytes = new byte[8];
        char[] destination = new char[100];

        bool ok = Base16.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(16, charsWritten);

        // Trailing positions beyond charsWritten must not be touched.
        for (int i = charsWritten; i < destination.Length; i++)
        {
            Assert.AreEqual('\0', destination[i],
                $"Position {i} beyond charsWritten should remain untouched.");
        }
    }
}
