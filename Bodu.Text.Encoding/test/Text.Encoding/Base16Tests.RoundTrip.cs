// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
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
        BaseFormattingOptions encodeOptions = (BaseFormattingOptions)encodeFlags;
        BaseFormatStyles decodeStyle = BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace;

        foreach (byte[] sample in EnumerateSamples())
        {
            string encoded = Base16.Encode(sample, encodeOptions);
            byte[] decoded = Base16.Decode(encoded, decodeStyle);

            CollectionAssert.AreEqual(sample, decoded,
                $"Round trip failed for encodeOptions={encodeOptions}, sample length={sample.Length}.");
        }
    }

    /// <summary>
    /// Verifies that strict-mode encode-then-decode round trips the canonical input.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenStrictEncodeAndDecode_ShouldRecoverOriginal()
    {
        string encoded = Base16.Encode(CanonicalBytes);
        byte[] decoded = Base16.Decode(encoded);

        CollectionAssert.AreEqual(CanonicalBytes, decoded);
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base16.TryEncode" /> + <see cref="Base16.TryDecode" /> round trip
    /// recovers the canonical input.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        char[] charBuffer = new char[CanonicalBytes.Length * 2];
        byte[] byteBuffer = new byte[CanonicalBytes.Length];

        bool encOk = Base16.TryEncode(CanonicalBytes.AsSpan(), charBuffer, out int charsWritten);
        bool decOk = Base16.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(CanonicalBytes.Length, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, byteBuffer);
    }

    private static IEnumerable<byte[]> EnumerateSamples()
    {
        yield return new byte[] { 0x00 };
        yield return new byte[] { 0xFF };
        yield return new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        yield return new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        byte[] sequential = new byte[64];
        for (int i = 0; i < sequential.Length; i++)
        {
            sequential[i] = (byte)i;
        }

        yield return sequential;

        byte[] all = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            all[i] = (byte)i;
        }

        yield return all;
    }
}
