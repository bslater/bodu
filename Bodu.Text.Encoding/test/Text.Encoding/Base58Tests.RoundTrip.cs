// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that every single byte value round-trips through both variants. Tagged Regression because of the
    /// 256 * 2 exhaustive sweep.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void RoundTrip_ForEverySingleByteValueAndVariant_ShouldRecover(Base58Variant variant)
    {
        for (int value = 0; value <= 255; value++)
        {
            byte[] original = new byte[] { (byte)value };

            string encoded = Base58.Encode(original, variant);
            byte[] decoded = Base58.Decode(encoded, variant);

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2}, variant={variant}.");
        }
    }
    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for both variants across a span of input
    /// sizes and patterns.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [TestMethod]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void RoundTrip_ForEveryVariantAcrossLengths_ShouldRecoverOriginalBytes(Base58Variant variant)
    {
        for (int len = 0; len <= 32; len++)
        {
            byte[] original = new byte[len];
            for (int i = 0; i < len; i++)
            {
                original[i] = (byte)((i * 13) + 7);
            }

            string encoded = Base58.Encode(original, variant);
            byte[] decoded = Base58.Decode(encoded, variant);

            CollectionAssert.AreEqual(original, decoded,
                $"Round trip failed for variant={variant}, length={len}.");
        }
    }

    /// <summary>
    /// Verifies that round-tripping all-zero input preserves length and recovers the original zero bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAllZeroInput_ShouldPreserveLength()
    {
        for (int len = 1; len <= 10; len++)
        {
            byte[] original = new byte[len];
            string encoded = Base58.Encode(original);
            byte[] decoded = Base58.Decode(encoded);

            Assert.AreEqual(len, decoded.Length, $"Length mismatch for {len} zero bytes.");
            CollectionAssert.AreEqual(original, decoded);
        }
    }

    /// <summary>
    /// Verifies that a typical Bitcoin address-length payload (25 bytes) round-trips correctly. The leading
    /// version byte 0x00 (mainnet P2PKH) ensures leading-zero handling is exercised.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenBitcoinAddressLengthInput_ShouldRecover()
    {
        byte[] original = new byte[25];
        original[0] = 0x00; // Bitcoin mainnet P2PKH version byte
        for (int i = 1; i < 25; i++)
        {
            original[i] = (byte)((i * 17) ^ 0xC3);
        }

        string encoded = Base58.Encode(original);
        byte[] decoded = Base58.Decode(encoded);

        CollectionAssert.AreEqual(original, decoded);
        Assert.IsTrue(encoded.StartsWith('1'), "P2PKH address starts with a leading '1' for the 0x00 version byte.");
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        char[] charBuffer = new char[Base58.GetMaxEncodedLength(original.Length)];
        byte[] byteBuffer = new byte[Base58.GetMaxDecodedLength(charBuffer.Length)];

        bool encOk = Base58.TryEncode(original.AsSpan(), charBuffer, out int charsWritten);
        bool decOk = Base58.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }

}
