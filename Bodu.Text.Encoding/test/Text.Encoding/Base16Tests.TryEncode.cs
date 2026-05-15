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
}
