// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.Length.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.GetEncodedLength(ReadOnlySpan{byte}, QuotedPrintableEncodingOptions)" />
    /// equals the characters written by the encoder across modes and content shapes.
    /// </summary>
    /// <param name="text">The ASCII source text.</param>
    /// <param name="textMode">Whether to use text mode.</param>
    [TestMethod]
    [DataRow("Hello, World!", false)]
    [DataRow("a b\tc ", false)]
    [DataRow("=== escapes ===", false)]
    [DataRow("line1\r\nline2\r\n", true)]
    [DataRow("", false)]
    public void GetEncodedLength_ShouldEqualTryEncodeCharsWritten(string text, bool textMode)
    {
        byte[] bytes = Ascii(text);
        QuotedPrintableEncodingOptions options = textMode ? TextOptions() : default;

        int reported = QuotedPrintable.GetEncodedLength(bytes, options);

        Span<char> destination = new char[QuotedPrintable.GetMaxEncodedLength(bytes.Length, options)];
        QuotedPrintable.TryEncode(bytes, destination, out int charsWritten, options);

        Assert.AreEqual(charsWritten, reported);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.GetEncodedLength(ReadOnlySpan{byte}, QuotedPrintableEncodingOptions)" />
    /// equals the length of the string produced by <see cref="QuotedPrintable.Encode(ReadOnlySpan{byte}, QuotedPrintableEncodingOptions)" />
    /// for a long wrapped payload.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenWrappedPayload_ShouldEqualEncodeLength()
    {
        byte[] bytes = Ascii(new string('A', 500));

        int reported = QuotedPrintable.GetEncodedLength(bytes);
        string encoded = QuotedPrintable.Encode(bytes);

        Assert.AreEqual(encoded.Length, reported);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.TryGetDecodedLength(ReadOnlySpan{char}, out int, QuotedPrintableDecodingOptions)" />
    /// equals the bytes written by the decoder.
    /// </summary>
    /// <param name="encoded">The Quoted-Printable input.</param>
    [TestMethod]
    [DataRow("Hello")]
    [DataRow("=3D=0C")]
    [DataRow("abc=\r\ndef")]
    [DataRow("a\r\nb")]
    public void TryGetDecodedLength_ShouldEqualTryDecodeBytesWritten(string encoded)
    {
        bool gotLength = QuotedPrintable.TryGetDecodedLength(encoded.AsSpan(), out int reported);

        Span<byte> destination = new byte[QuotedPrintable.GetMaxDecodedLength(encoded.Length)];
        bool decoded = QuotedPrintable.TryDecode(encoded.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(gotLength);
        Assert.IsTrue(decoded);
        Assert.AreEqual(bytesWritten, reported);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.GetMaxDecodedLength(int)" /> returns the input character count.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_ShouldReturnCharCount()
    {
        Assert.AreEqual(42, QuotedPrintable.GetMaxDecodedLength(42));
    }
}
