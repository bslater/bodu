// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> rolls back <c>bytesConsumed</c> and <c>bytesWritten</c> to
    /// the last completed 4-symbol / 3-byte quantum boundary when the destination cannot accommodate the next quantum.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportLastQuantumBoundary()
    {
        // "Zm9vYmFy" decodes to "foobar" (6 bytes). A 3-byte destination fits only the first quantum.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[3];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(4, bytesConsumed);
        Assert.AreEqual(3, bytesWritten);

        byte[] expected = Ascii("foo");
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination is undersized.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[1];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> in non-final-block mode returns
    /// <see cref="OperationStatus.NeedMoreData" /> for mid-group input.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenMidGroupAndNotFinal_ShouldReturnNeedMoreData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9v"); // 4 chars - complete
        byte[] partial = System.Text.Encoding.ASCII.GetBytes("Zm9vYmE"); // 7 chars - mid-group
        byte[] destination = new byte[6];

        OperationStatus statusComplete = Base64.DecodeFromUtf8(utf8, destination, out _, out _, isFinalBlock: false);
        Assert.AreEqual(OperationStatus.Done, statusComplete);

        OperationStatus statusPartial = Base64.DecodeFromUtf8(partial, destination, out _, out _, isFinalBlock: false);
        Assert.AreEqual(OperationStatus.NeedMoreData, statusPartial);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> reports <see cref="OperationStatus.NeedMoreData" /> with
    /// quantum-aligned counters when the streaming input ends mid-quartet.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenNeedMoreData_ShouldReportLastQuantumBoundary()
    {
        // "Zm9vYg" is one full quartet ("Zm9v" = "foo") + 2 partial chars.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYg");
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
        Assert.AreEqual(4, bytesConsumed);
        Assert.AreEqual(3, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> recovers bytes from a UTF-8 Base64 input and reports
    /// counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenStrictValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> with the UrlSafe variant accepts the substituted alphabet.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenUrlSafeVariant_ShouldDecodeSubstitutedAlphabet()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("-__-");
        byte[] destination = new byte[3];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out int bytesWritten, Base64Variant.UrlSafe);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(3, bytesWritten);
    }
    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> reports <see cref="OperationStatus.InvalidData" /> for padded
    /// UTF-8 input shorter than one four-character quantum.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenStrictInputIsPaddedAndShorterThanAQuantum_ShouldReturnInvalidData()
    {
        byte[] destination = new byte[4];

        OperationStatus status = Base64.DecodeFromUtf8(Ascii("QQ="), destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.InvalidData, status);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> returns the Standard variant output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfStandardOutput()
    {
        byte[] actual = Base64.EncodeToUtf8(Ascii("foobar"));

        Assert.AreEqual("Zm9vYmFy", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        byte[] actual = Base64.EncodeToUtf8([]);

        Assert.IsEmpty(actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> with <see cref="BaseFormattingOptions.OmitPadding" /> drops
    /// padding bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenOmitPadding_ShouldNotEmitPaddingBytes()
    {
        byte[] actual = Base64.EncodeToUtf8(Ascii("foob"), Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("Zm9vYg", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> with <see cref="Base64Variant.UrlSafe" /> swaps the alphabet
    /// characters.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenUrlSafeVariant_ShouldSwapAlphabetCharacters()
    {
        byte[] bytes = new byte[] { 0xFB, 0xFF, 0xFE };

        byte[] actual = Base64.EncodeToUtf8(bytes, Base64Variant.UrlSafe);

        string encoded = System.Text.Encoding.ASCII.GetString(actual);
        Assert.IsFalse(encoded.Contains('+'));
        Assert.IsFalse(encoded.Contains('/'));
    }

    /// <summary>
    /// Verifies that round-trip via the UTF-8 path recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        byte[] original = Ascii("foobar");
        byte[] encoded = Base64.EncodeToUtf8(original);
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(encoded, destination, out _, out _);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(original, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> writes the Standard variant output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[8];

        bool ok = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual("Zm9vYmFy", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> returns <see langword="false" /> when destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] destination = new byte[1];

        bool ok = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> rejects unsupported flags.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenUnsupportedFlag_ShouldThrowExactly()
    {
        byte[] destination = new byte[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out _, Base64Variant.Standard, BaseFormattingOptions.InsertLineBreaks);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" /> with
    /// the MIME variant writes the CRLF line breaks every 76 characters, so its bytes equal the ASCII bytes of
    /// <see cref="Base64.Encode(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" /> for payloads that fill
    /// none, one, and several lines, padded and unpadded.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenMimeVariant_ShouldEqualEncodeOutputBytes()
    {
        var mismatches = new List<string>();

        foreach ((byte[] payload, BaseFormattingOptions options) in GetMimeUtf8Cases())
        {
            byte[] expected = Ascii(Base64.Encode(payload, Base64Variant.Mime, options));
            byte[] actual = Base64.EncodeToUtf8(payload, Base64Variant.Mime, options);
            if (!actual.AsSpan().SequenceEqual(expected))
                mismatches.Add($"{payload.Length} bytes, {options}");
        }

        Assert.AreEqual(0, mismatches.Count, string.Join("; ", mismatches));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base64Variant, BaseFormattingOptions)" />
    /// with the MIME variant writes the same bytes as the ASCII bytes of
    /// <see cref="Base64.Encode(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" />, line breaks included.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenBufferWriterAndMimeVariant_ShouldEqualEncodeOutputBytes()
    {
        var mismatches = new List<string>();

        foreach ((byte[] payload, BaseFormattingOptions options) in GetMimeUtf8Cases())
        {
            byte[] expected = Ascii(Base64.Encode(payload, Base64Variant.Mime, options));
            var writer = new ArrayBufferWriter<byte>();
            int written = Base64.EncodeToUtf8(payload, writer, Base64Variant.Mime, options);
            if (written != expected.Length || !writer.WrittenSpan.SequenceEqual(expected))
                mismatches.Add($"{payload.Length} bytes, {options}");
        }

        Assert.AreEqual(0, mismatches.Count, string.Join("; ", mismatches));
    }

    /// <summary>
    /// Verifies that
    /// <see cref="Base64.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base64Variant, BaseFormattingOptions)" />
    /// with the MIME variant writes the same bytes as the ASCII bytes of
    /// <see cref="Base64.Encode(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" />, line breaks included,
    /// into a destination of exactly <see cref="Base64.GetEncodedLength(int, Base64Variant, BaseFormattingOptions)" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenMimeVariant_ShouldEqualEncodeOutputBytes()
    {
        var mismatches = new List<string>();

        foreach ((byte[] payload, BaseFormattingOptions options) in GetMimeUtf8Cases())
        {
            byte[] expected = Ascii(Base64.Encode(payload, Base64Variant.Mime, options));
            byte[] destination = new byte[Base64.GetEncodedLength(payload.Length, Base64Variant.Mime, options)];
            bool encoded = Base64.TryEncodeToUtf8(payload, destination, out int written, Base64Variant.Mime, options);
            if (!encoded || !destination.AsSpan(0, written).SequenceEqual(expected))
                mismatches.Add($"{payload.Length} bytes, {options}");
        }

        Assert.AreEqual(0, mismatches.Count, string.Join("; ", mismatches));
    }

    /// <summary>
    /// Returns the MIME UTF-8 cases: every payload length from zero to 240 bytes (none to four 76-character lines and a
    /// partial fifth), padded and unpadded.
    /// </summary>
    /// <returns>The payload and formatting option pairs.</returns>
    private static IEnumerable<(byte[] Payload, BaseFormattingOptions Options)> GetMimeUtf8Cases()
    {
        var random = new Random(0x3170);

        for (int length = 0; length <= 240; length++)
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);

            yield return (payload, BaseFormattingOptions.None);
            yield return (payload, BaseFormattingOptions.OmitPadding);
        }
    }

}
