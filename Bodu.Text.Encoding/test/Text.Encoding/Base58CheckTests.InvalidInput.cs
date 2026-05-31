// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58CheckTests.InvalidInput.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Negative-vector tests for <see cref="Base58Check" /> that complement the existing checksum-tamper cases in
/// <see cref="Base58CheckTests" />. The cases here corrupt the encoded payload rather than the checksum trailer
/// to exercise the alternate failure path: the checksum bytes are intact but no longer match the payload.
/// </summary>
[TestClass]
public sealed class Base58CheckTests_InvalidInput
{
    /// <summary>
    /// The 21-byte Bitcoin Genesis block payload (version byte plus HASH-160 of Satoshi's public key) — kept in
    /// sync with the constant in <see cref="Base58CheckTests" />.
    /// </summary>
    private static readonly byte[] GenesisPayload =
    [
        0x00,
        0x62, 0xE9, 0x07, 0xB1, 0x5C, 0xBF, 0x27, 0xD5, 0x42, 0x53,
        0x99, 0xEB, 0xF6, 0xF0, 0xFB, 0x50, 0xEB, 0xB8, 0x8F, 0x18,
    ];

    /// <summary>
    /// Produces a Base58Check string whose payload byte at <paramref name="index" /> has been XORed with
    /// <paramref name="mask" />, leaving the original checksum bytes unchanged so the resulting string fails the
    /// checksum equality check during decode.
    /// </summary>
    /// <param name="index">The payload-byte index to corrupt (0-based).</param>
    /// <param name="mask">The XOR mask used to flip bits in the chosen payload byte.</param>
    /// <returns>The corrupted Base58Check-encoded string.</returns>
    private static string MakePayloadCorruptedAddress(int index, byte mask)
    {
        var payload = (byte[])GenesisPayload.Clone();
        var canonical = Base58Check.Encode(payload);

        // Re-encode after corrupting the payload but reuse the original checksum bytes. Easiest path: decode raw
        // via Base58, mutate the payload slice, append the original checksum bytes, then re-encode.
        var decodedWithChecksum = Base58.Decode(canonical.AsSpan());
        decodedWithChecksum[index] ^= mask;
        return Base58.Encode(decodedWithChecksum);
    }

    /// <summary>
    /// Verifies that flipping bits in the encoded payload (while leaving the checksum bytes untouched) causes
    /// <see cref="Base58Check.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> to throw
    /// <see cref="FormatException" /> with a message referencing the checksum failure.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPayloadByteIsCorrupted_ShouldThrowFormatException()
    {
        var corrupted = MakePayloadCorruptedAddress(index: 1, mask: 0x01);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58Check.Decode(corrupted.AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("checksum", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> returns <see langword="false" /> for a payload-corrupted
    /// input and sets <c>bytesWritten</c> to zero.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenPayloadByteIsCorrupted_ShouldReturnFalse()
    {
        var corrupted = MakePayloadCorruptedAddress(index: 10, mask: 0x80);
        var destination = new byte[GenesisPayload.Length];

        var ok = Base58Check.TryDecode(corrupted.AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.IsValid" /> returns <see langword="false" /> when a payload byte is
    /// flipped.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenPayloadByteIsCorrupted_ShouldReturnFalse()
    {
        var corrupted = MakePayloadCorruptedAddress(index: 5, mask: 0x40);

        Assert.IsFalse(Base58Check.IsValid(corrupted.AsSpan()));
    }
}
