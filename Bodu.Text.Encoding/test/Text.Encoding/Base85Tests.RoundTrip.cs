// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for the Ascii85 variant across input
    /// sizes spanning all four residue classes mod 4.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(32)]
    public void RoundTrip_ForAscii85AcrossLengths_ShouldRecoverOriginalBytes(int byteCount)
    {
        var original = new byte[byteCount];
        for (var i = 0; i < byteCount; i++)
            original[i] = (byte)((i * 17) + 3);

        var encoded = Base85.Encode(original);
        var decoded = Base85.Decode(encoded);

        CollectionAssert.AreEqual(original, decoded, $"Round trip failed for length={byteCount}.");
    }

    /// <summary>
    /// Verifies that every single byte value round-trips through Ascii85. Tagged Regression because of the
    /// exhaustive 256-value sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_ForEverySingleByteValueAscii85_ShouldRecover()
    {
        for (var value = 0; value <= 255; value++)
        {
            var original = new byte[] { (byte)value };

            var encoded = Base85.Encode(original);
            var decoded = Base85.Decode(encoded);

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2}.");
        }
    }

    /// <summary>
    /// Verifies that an 8 KiB random buffer round-trips through both variants. Tagged Regression because of the
    /// 8 KiB sweep.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(Base85Variant.Ascii85)]
    [DataRow(Base85Variant.Z85)]
    public void RoundTrip_ForLargeRandomInput_ShouldRecover(Base85Variant variant)
    {
        var size = 8192;
        var original = new byte[size];
        new Random(0xBEEF).NextBytes(original);

        var encoded = Base85.Encode(original, variant);
        var decoded = Base85.Decode(encoded, variant);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that encode followed by decode recovers the original bytes for the Z85 variant.
    /// </summary>
    /// <param name="byteCount">The input byte count (must be a multiple of four).</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(32)]
    public void RoundTrip_ForZ85AlignedLengths_ShouldRecoverOriginalBytes(int byteCount)
    {
        var original = new byte[byteCount];
        for (var i = 0; i < byteCount; i++)
            original[i] = (byte)((i * 13) + 7);

        var encoded = Base85.Encode(original, Base85Variant.Z85);
        var decoded = Base85.Decode(encoded, Base85Variant.Z85);

        CollectionAssert.AreEqual(original, decoded, $"Z85 round trip failed for length={byteCount}.");
    }

    /// <summary>
    /// Verifies that round-trip preserves the maximum 32-bit value group (four 0xFF bytes) for Ascii85, decoded
    /// without triggering the <c>z</c> shortcut.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAllOnesGroupForAscii85_ShouldRecover()
    {
        var original = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        var encoded = Base85.Encode(original);
        var decoded = Base85.Decode(encoded);

        Assert.AreEqual(5, encoded.Length);
        Assert.AreNotEqual("z", encoded); // 0xFFFFFFFF is not the all-zero shortcut
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that Ascii85 round trips inputs containing all-zero 4-byte groups via the <c>z</c> shortcut.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAllZeroGroupsAndAscii85_ShouldPreserveBytes()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x12, 0x34, 0x56, 0x78 };

        var encoded = Base85.Encode(original);
        Assert.IsTrue(encoded.Contains('z'), "All-zero group should be encoded with the 'z' shortcut.");

        var decoded = Base85.Decode(encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes for Ascii85.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPathForAscii85_ShouldRecoverOriginal()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        var charBuffer = new char[Base85.GetMaxEncodedLength(original.Length)];
        var byteBuffer = new byte[Base85.GetMaxDecodedLength(charBuffer.Length)];

        var encOk = Base85.TryEncode(original.AsSpan(), charBuffer, out var charsWritten);
        var decOk = Base85.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out var bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }

}
