// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for both variants across a span of input
    /// sizes and patterns.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [DataTestMethod]
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
