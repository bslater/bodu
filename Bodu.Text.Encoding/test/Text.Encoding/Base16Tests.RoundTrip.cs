// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.RoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that encoding then decoding every single byte value from <c>0x00</c> through <c>0xFF</c> recovers
    /// the original byte. Tagged Regression because it is an exhaustive single-byte sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void EncodeDecode_ForEverySingleByteValue_ShouldRoundTrip()
    {
        for (var value = 0; value <= 255; value++)
        {
            var original = new byte[] { (byte)value };

            var encoded = Base16.Encode(original);
            var decoded = Base16.Decode(encoded);

            Assert.AreEqual(2, encoded.Length, $"Encoded length should be 2 for byte 0x{value:X2}.");
            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2}.");
        }
    }

    /// <summary>
    /// Verifies that every option combination produces an output that decodes back to the original bytes for a
    /// curated set of byte patterns. Tagged Regression because it is a full flag-combination sweep.
    /// </summary>
    /// <param name="patternKey">A key identifying a byte pattern in <see cref="GetPatternBytes" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("zero-bytes")]
    [DataRow("max-bytes")]
    [DataRow("ascending")]
    [DataRow("descending")]
    [DataRow("alternating-aa-55")]
    [DataRow("alternating-0f-f0")]
    [DataRow("ascii-printable")]
    [DataRow("high-bytes")]
    public void RoundTrip_ForCommonBytePatterns_ShouldRecoverOriginal(string patternKey)
    {
        var original = GetPatternBytes(patternKey);
        BaseFormatStyles decodeStyle = BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace;

        for (var flags = 0; flags <= 0x0F; flags++)
        {
            var options = (BaseFormattingOptions)flags;

            var encoded = Base16.Encode(original, options);
            var decoded = Base16.Decode(encoded, decodeStyle);

            CollectionAssert.AreEqual(original, decoded,
                $"Pattern={patternKey}, options={options}: round trip failed.");
        }
    }
    /// <summary>
    /// Verifies that an encode-then-decode round trip recovers the original bytes for representative inputs across
    /// every combination of <see cref="BaseFormattingOptions" /> flags paired with the matching
    /// <see cref="BaseFormatStyles" />.
    /// </summary>
    /// <param name="encodeFlags">The encode options applied to the encoder.</param>
    [DataTestMethod]
    [DataRow((byte)BaseFormattingOptions.None)]
    [DataRow((byte)BaseFormattingOptions.UpperCase)]
    [DataRow((byte)BaseFormattingOptions.IncludePrefix)]
    [DataRow((byte)BaseFormattingOptions.InsertSpacing)]
    [DataRow((byte)BaseFormattingOptions.InsertLineBreaks)]
    [DataRow((byte)(BaseFormattingOptions.UpperCase | BaseFormattingOptions.IncludePrefix))]
    [DataRow((byte)(BaseFormattingOptions.UpperCase | BaseFormattingOptions.InsertSpacing))]
    [DataRow((byte)(BaseFormattingOptions.UpperCase | BaseFormattingOptions.InsertLineBreaks))]
    [DataRow((byte)(BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertSpacing))]
    [DataRow((byte)(BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertLineBreaks))]
    [DataRow((byte)(BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.InsertLineBreaks))]
    [DataRow((byte)(BaseFormattingOptions.UpperCase | BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.InsertLineBreaks))]
    public void RoundTrip_ForEveryFormattingOptionsCombination_ShouldRecoverOriginalBytes(byte encodeFlags)
    {
        var encodeOptions = (BaseFormattingOptions)encodeFlags;
        BaseFormatStyles decodeStyle = BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace;

        foreach (var sample in EnumerateSamples())
        {
            var encoded = Base16.Encode(sample, encodeOptions);
            var decoded = Base16.Decode(encoded, decodeStyle);

            CollectionAssert.AreEqual(sample, decoded,
                $"Round trip failed for encodeOptions={encodeOptions}, sample length={sample.Length}.");
        }
    }

    /// <summary>
    /// Verifies that large inputs (4 KiB to 64 KiB) round-trip cleanly through both the string-returning encoder
    /// and the span-based encoder/decoder. Tagged Regression because of the high iteration cost.
    /// </summary>
    /// <param name="size">The size of the input buffer.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(4_096)]
    [DataRow(16_384)]
    [DataRow(65_536)]
    public void RoundTrip_ForLargeInputs_ShouldRecoverOriginal(int size)
    {
        var original = new byte[size];
        Random rng = new(0x12345678);
        rng.NextBytes(original);

        var stringEncoded = Base16.Encode(original);
        var stringDecoded = Base16.Decode(stringEncoded);
        CollectionAssert.AreEqual(original, stringDecoded);

        var spanCharBuffer = new char[size * 2];
        var spanByteBuffer = new byte[size];
        Assert.IsTrue(Base16.TryEncode(original.AsSpan(), spanCharBuffer, out var charsWritten));
        Assert.IsTrue(Base16.TryDecode(spanCharBuffer.AsSpan(0, charsWritten), spanByteBuffer, out var bytesWritten));
        Assert.AreEqual(size, bytesWritten);
        CollectionAssert.AreEqual(original, spanByteBuffer);
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base16.TryEncode" /> + <see cref="Base16.TryDecode" /> round trip
    /// recovers the canonical input.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        var charBuffer = new char[CanonicalBytes.Length * 2];
        var byteBuffer = new byte[CanonicalBytes.Length];

        var encOk = Base16.TryEncode(CanonicalBytes.AsSpan(), charBuffer, out var charsWritten);
        var decOk = Base16.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out var bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(CanonicalBytes.Length, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, byteBuffer);
    }

    /// <summary>
    /// Verifies that strict-mode encode-then-decode round trips the canonical input.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenStrictEncodeAndDecode_ShouldRecoverOriginal()
    {
        var encoded = Base16.Encode(CanonicalBytes);
        var decoded = Base16.Decode(encoded);

        CollectionAssert.AreEqual(CanonicalBytes, decoded);
    }

    /// <summary>
    /// Verifies that the upper-case encoder produces output that the default (lower-case-preferring) decoder accepts
    /// across every byte value — Base16 decoding is case-insensitive. Tagged Regression because it covers the full
    /// byte range.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_WhenUpperCaseEncodedAndDecodedAsLowerCase_ShouldRecoverOriginal()
    {
        var original = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            original[i] = (byte)i;
        }

        var upper = Base16.Encode(original, BaseFormattingOptions.UpperCase);
        var lower = upper.ToLowerInvariant();

        var decodedUpper = Base16.Decode(upper);
        var decodedLower = Base16.Decode(lower);

        CollectionAssert.AreEqual(original, decodedUpper);
        CollectionAssert.AreEqual(original, decodedLower);
    }

    private static IEnumerable<byte[]> EnumerateSamples()
    {
        yield return new byte[] { 0x00 };
        yield return new byte[] { 0xFF };
        yield return new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        yield return new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        var sequential = new byte[64];
        for (var i = 0; i < sequential.Length; i++)
        {
            sequential[i] = (byte)i;
        }

        yield return sequential;

        var all = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            all[i] = (byte)i;
        }

        yield return all;
    }

}
