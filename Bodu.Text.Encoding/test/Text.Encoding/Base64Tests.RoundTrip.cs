// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that every single byte value round-trips through all three variants. Tagged Regression because the
    /// 256 * 3 sweep is exhaustive.
    /// </summary>
    /// <param name="variant">The Base64 variant.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void RoundTrip_ForEverySingleByteValueAndVariant_ShouldRecover(Base64Variant variant)
    {
        for (int value = 0; value <= 255; value++)
        {
            byte[] original = new byte[] { (byte)value };

            string encoded = Base64.Encode(original, variant);
            byte[] decoded = Base64.Decode(encoded, variant);

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2}, variant={variant}.");
        }
    }
    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for every variant across input sizes
    /// spanning all four residue classes mod 3.
    /// </summary>
    /// <param name="variant">The Base64 variant.</param>
    [TestMethod]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void RoundTrip_ForEveryVariantAcrossLengths_ShouldRecoverOriginalBytes(Base64Variant variant)
    {
        for (int len = 0; len <= 64; len++)
        {
            byte[] original = new byte[len];
            for (int i = 0; i < len; i++)
            {
                original[i] = (byte)(i * 7);
            }

            string encoded = Base64.Encode(original, variant);
            byte[] decoded = Base64.Decode(encoded, variant);

            CollectionAssert.AreEqual(original, decoded,
                $"Round trip failed for variant={variant}, length={len}.");
        }
    }

    /// <summary>
    /// Verifies that an 8 KiB random buffer round-trips through every variant. Tagged Regression because of the
    /// per-variant 8 KiB sweep.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void RoundTrip_ForLargeRandomInput_ShouldRecover(Base64Variant variant)
    {
        byte[] original = new byte[8192];
        new Random(0x42).NextBytes(original);

        string encoded = Base64.Encode(original, variant);
        byte[] decoded = Base64.Decode(encoded, variant);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that round-tripping with <see cref="BaseFormattingOptions.OmitPadding" /> on encode and
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> on decode recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenOmitPaddingAndAllowMissingPadding_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string encoded = Base64.Encode(original, Base64Variant.Standard, BaseFormattingOptions.OmitPadding);
        byte[] decoded = Base64.Decode(encoded, Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding);

        Assert.IsFalse(encoded.Contains('='), "Encoded form should contain no padding characters.");
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        char[] charBuffer = new char[Base64.GetEncodedLength(original.Length)];
        byte[] byteBuffer = new byte[original.Length];

        bool encOk = Base64.TryEncode(original.AsSpan(), charBuffer, out int charsWritten);
        bool decOk = Base64.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer);
    }

}
