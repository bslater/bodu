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
        byte[] original = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            original[i] = (byte)((i * 17) + 3);

        string encoded = Base85.Encode(original);
        byte[] decoded = Base85.Decode(encoded);

        CollectionAssert.AreEqual(original, decoded, $"Round trip failed for length={byteCount}.");
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
        byte[] original = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            original[i] = (byte)((i * 13) + 7);

        string encoded = Base85.Encode(original, Base85Variant.Z85);
        byte[] decoded = Base85.Decode(encoded, Base85Variant.Z85);

        CollectionAssert.AreEqual(original, decoded, $"Z85 round trip failed for length={byteCount}.");
    }

    /// <summary>
    /// Verifies that Ascii85 round trips inputs containing all-zero 4-byte groups via the <c>z</c> shortcut.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAllZeroGroupsAndAscii85_ShouldPreserveBytes()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00, 0x12, 0x34, 0x56, 0x78 };

        string encoded = Base85.Encode(original);
        Assert.IsTrue(encoded.Contains('z'), "All-zero group should be encoded with the 'z' shortcut.");

        byte[] decoded = Base85.Decode(encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes for Ascii85.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPathForAscii85_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        char[] charBuffer = new char[Base85.GetMaxEncodedLength(original.Length)];
        byte[] byteBuffer = new byte[Base85.GetMaxDecodedLength(charBuffer.Length)];

        bool encOk = Base85.TryEncode(original.AsSpan(), charBuffer, out int charsWritten);
        bool decOk = Base85.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }
}
