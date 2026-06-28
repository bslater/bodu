// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that every single byte value round-trips through binary-mode encode and strict decode. Tagged
    /// Regression because of the exhaustive 256-value sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_ForEverySingleByteValueBinary_ShouldRecover()
    {
        for (int value = 0; value <= 255; value++)
        {
            byte[] original = { (byte)value };

            string encoded = QuotedPrintable.Encode(original);
            byte[] decoded = QuotedPrintable.Decode(encoded.AsSpan());

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2}.");
        }
    }

    /// <summary>
    /// Verifies that random binary payloads of lengths 0 through 1024 round-trip through binary mode. Tagged
    /// Regression because of the length sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_ForRandomBinaryLengths_ShouldRecover()
    {
        var random = new Random(0x4D1E);
        for (int length = 0; length <= 1024; length++)
        {
            byte[] original = new byte[length];
            random.NextBytes(original);

            string encoded = QuotedPrintable.Encode(original);
            byte[] decoded = QuotedPrintable.Decode(encoded.AsSpan());

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for length={length}.");
        }
    }

    /// <summary>
    /// Verifies that mostly-ASCII text containing tabs, spaces, and long runs round-trips through binary mode without
    /// emitting decoder-rejected trailing whitespace.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenMixedAsciiTextWithSpacesAndLongLines_ShouldRecover()
    {
        byte[] original = Ascii("The quick brown fox \tjumps over the lazy dog. " + new string('x', 200) + "  trailing spaces   ");

        string encoded = QuotedPrintable.Encode(original);
        byte[] decoded = QuotedPrintable.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that text-mode canonical content round-trips when decoded as bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenTextModeCanonicalContent_ShouldRecover()
    {
        byte[] original = Ascii("First line with = and é-like \tcontent.\r\nSecond line.\r\n");

        string encoded = QuotedPrintable.Encode(original, TextOptions());
        byte[] decoded = QuotedPrintable.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that span-based <see cref="QuotedPrintable.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, QuotedPrintableEncodingOptions)" />
    /// and <see cref="QuotedPrintable.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, QuotedPrintableDecodingOptions)" />
    /// round-trip through pre-sized buffers.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        byte[] original = { 0x00, 0x3D, 0x20, 0x09, 0xFF, 0x41, 0x0D, 0x0A };
        char[] charBuffer = new char[QuotedPrintable.GetMaxEncodedLength(original.Length)];

        bool encOk = QuotedPrintable.TryEncode(original, charBuffer, out int charsWritten);
        byte[] byteBuffer = new byte[QuotedPrintable.GetMaxDecodedLength(charsWritten)];
        bool decOk = QuotedPrintable.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }
}
