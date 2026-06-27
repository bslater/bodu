// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that the default options normalise to binary mode, 76-column wrapping, and a CRLF newline.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDefaultOptions_ShouldUseBinaryModeAndCrlf()
    {
        // CR/LF are escaped (binary mode), so a CRLF source pair becomes =0D=0A rather than a hard break.
        string encoded = QuotedPrintable.Encode(Ascii("a\r\nb"));

        Assert.AreEqual("a=0D=0Ab", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintableEncodingOptions.Default" /> exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Default_ShouldExposeBinaryMode76ColumnCrlf()
    {
        QuotedPrintableEncodingOptions options = QuotedPrintableEncodingOptions.Default;

        Assert.AreEqual(QuotedPrintableEncodingMode.Binary, options.Mode);
        Assert.AreEqual(76, options.MaxLineLength);
        Assert.AreEqual("\r\n", options.NewLine);
    }

    /// <summary>
    /// Verifies that a zero <see cref="QuotedPrintableEncodingOptions.MaxLineLength" /> is normalised to the RFC
    /// default of 76 rather than meaning unlimited.
    /// </summary>
    [TestMethod]
    public void Encode_WhenMaxLineLengthZero_ShouldWrapAt76()
    {
        var options = new QuotedPrintableEncodingOptions(QuotedPrintableEncodingMode.Binary, 0, "\r\n");

        string encoded = QuotedPrintable.Encode(Ascii(new string('A', 200)), options);

        foreach (string line in encoded.Split("\r\n"))
            Assert.IsTrue(line.Length <= 76, $"Line exceeded 76 characters: '{line}'.");
    }

    /// <summary>
    /// Verifies that a <see cref="QuotedPrintableEncodingOptions.MaxLineLength" /> below four is rejected.
    /// </summary>
    [TestMethod]
    public void Encode_WhenMaxLineLengthBelowFour_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new QuotedPrintableEncodingOptions(QuotedPrintableEncodingMode.Binary, 3, "\r\n");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = QuotedPrintable.Encode(Ascii("test"), options);
        });
    }

    /// <summary>
    /// Verifies that an unsupported newline string is rejected.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNewLineUnsupported_ShouldThrowArgumentException()
    {
        var options = new QuotedPrintableEncodingOptions(QuotedPrintableEncodingMode.Binary, 76, "\r");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = QuotedPrintable.Encode(Ascii("test"), options);
        });
    }

    /// <summary>
    /// Verifies that an undefined <see cref="QuotedPrintableEncodingMode" /> is rejected.
    /// </summary>
    [TestMethod]
    public void Encode_WhenModeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new QuotedPrintableEncodingOptions((QuotedPrintableEncodingMode)42, 76, "\r\n");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = QuotedPrintable.Encode(Ascii("test"), options);
        });
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, QuotedPrintableEncodingOptions)" />
    /// returns <see langword="false" /> for invalid options without throwing.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenOptionsInvalid_ShouldReturnFalse()
    {
        var options = new QuotedPrintableEncodingOptions(QuotedPrintableEncodingMode.Binary, 2, "\r\n");
        Span<char> destination = new char[64];

        bool success = QuotedPrintable.TryEncode(Ascii("test"), destination, out int charsWritten, options);

        Assert.IsFalse(success);
        Assert.AreEqual(0, charsWritten);
    }
}
