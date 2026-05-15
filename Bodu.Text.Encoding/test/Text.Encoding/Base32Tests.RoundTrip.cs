// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for every variant across a span of input
    /// sizes spanning all five residue classes mod 5.
    /// </summary>
    /// <param name="variant">The Base32 variant.</param>
    [DataTestMethod]
    [DataRow(Base32Variant.Standard)]
    [DataRow(Base32Variant.HexExtended)]
    [DataRow(Base32Variant.Crockford)]
    [DataRow(Base32Variant.ZBase32)]
    public void RoundTrip_ForEveryVariantAcrossLengths_ShouldRecoverOriginalBytes(Base32Variant variant)
    {
        for (int len = 0; len <= 20; len++)
        {
            byte[] original = new byte[len];
            for (int i = 0; i < len; i++)
            {
                original[i] = (byte)(i * 7);
            }

            string encoded = Base32.Encode(original, variant);
            byte[] decoded = Base32.Decode(encoded, variant);

            CollectionAssert.AreEqual(original, decoded,
                $"Round trip failed for variant={variant}, length={len}.");
        }
    }

    /// <summary>
    /// Verifies that round-tripping with <see cref="BaseFormattingOptions.OmitPadding" /> on the encode side and
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> on the decode side recovers the original bytes for the
    /// Standard variant.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenOmitPaddingAndAllowMissingPadding_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string encoded = Base32.Encode(original, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);
        byte[] decoded = Base32.Decode(encoded, Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding);

        CollectionAssert.AreEqual(original, decoded);
        Assert.IsFalse(encoded.Contains('='), "Encoded form should contain no padding characters.");
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        char[] charBuffer = new char[Base32.GetEncodedLength(original.Length)];
        byte[] byteBuffer = new byte[original.Length];

        bool encOk = Base32.TryEncode(original.AsSpan(), charBuffer, out int charsWritten);
        bool decOk = Base32.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer);
    }
}
