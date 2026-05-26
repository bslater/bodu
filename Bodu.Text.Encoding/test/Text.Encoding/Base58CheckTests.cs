// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58CheckTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Tests for the <see cref="Base58Check" /> encoder and decoder, covering the SHA-256 squared checksum semantics,
/// round-trip behaviour, and tamper detection.
/// </summary>
[TestClass]
public sealed class Base58CheckTests
{

    /// <summary>
    /// Known answer test vector from the Bitcoin Genesis block coinbase address. The input is the 21-byte
    /// representation of the address (version byte 0x00 + 20-byte HASH-160 of Satoshi's public key).
    /// </summary>
    private const string GenesisAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";

    /// <summary>
    /// The 21-byte payload represented by <see cref="GenesisAddress" /> — a version byte (0x00) followed by the
    /// 20-byte HASH-160 of the Genesis block public key.
    /// </summary>
    private static readonly byte[] GenesisPayload =
    [
        0x00,
        0x62, 0xE9, 0x07, 0xB1, 0x5C, 0xBF, 0x27, 0xD5, 0x42, 0x53,
        0x99, 0xEB, 0xF6, 0xF0, 0xFB, 0x50, 0xEB, 0xB8, 0x8F, 0x18,
    ];

    /// <summary>
    /// Verifies that <see cref="Base58Check.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> recovers
    /// the original 21-byte payload from the Bitcoin Genesis block address.
    /// </summary>
    [TestMethod]
    public void Decode_ForGenesisAddress_ShouldRecoverGenesisPayload()
    {
        var decoded = Base58Check.Decode(GenesisAddress.AsSpan());

        CollectionAssert.AreEqual(GenesisPayload, decoded);
    }

    /// <summary>
    /// Verifies that a Base58Check input whose checksum has been tampered with throws <see cref="FormatException" />
    /// when passed to <see cref="Base58Check.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenChecksumIsCorrupted_ShouldThrowExactly()
    {
        // Flip the last character of the Genesis address — almost certainly invalidates the checksum.
        var tampered = GenesisAddress.ToCharArray();
        tampered[^1] = tampered[^1] == 'a' ? 'b' : 'a';
        var tamperedAddress = new string(tampered);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58Check.Decode(tamperedAddress.AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("checksum", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> when the input is shorter than the checksum suffix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputTooShortForChecksum_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58Check.Decode("1".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("checksum", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.Encode(ReadOnlySpan{byte}, Base58Variant)" /> produces the canonical
    /// Bitcoin Genesis block address from the corresponding 21-byte payload.
    /// </summary>
    [TestMethod]
    public void Encode_ForGenesisBlockPayload_ShouldProduceGenesisAddress()
    {
        var encoded = Base58Check.Encode(GenesisPayload);

        Assert.AreEqual(GenesisAddress, encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.GetMaxEncodedLength(int)" /> returns a bound large enough to fit the
    /// actual encoded output.
    /// </summary>
    [TestMethod]
    public void GetMaxEncodedLength_ShouldReturnUpperBoundLargeEnoughForOutput()
    {
        var payload = new byte[20];
        new Random(0xABCDEF).NextBytes(payload);

        var upper = Base58Check.GetMaxEncodedLength(payload.Length);
        var encoded = Base58Check.Encode(payload);

        Assert.IsTrue(encoded.Length <= upper);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.IsValid" /> returns <see langword="true" /> for a structurally and
    /// cryptographically valid Base58Check encoding.
    /// </summary>
    [TestMethod]
    public void IsValid_ForGenesisAddress_ShouldReturnTrue()
    {
        Assert.IsTrue(Base58Check.IsValid(GenesisAddress.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.IsValid" /> returns <see langword="false" /> when the checksum bytes are
    /// tampered with.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenChecksumIsCorrupted_ShouldReturnFalse()
    {
        var tampered = GenesisAddress.ToCharArray();
        tampered[^1] = tampered[^1] == 'a' ? 'b' : 'a';

        Assert.IsFalse(Base58Check.IsValid(new string(tampered).AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.IsValid" /> returns <see langword="false" /> for an input whose decoded
    /// length is shorter than the checksum length.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputTooShortForChecksum_ShouldReturnFalse()
    {
        // A single "1" decodes to a single zero byte — fewer bytes than the 4-byte checksum suffix.
        Assert.IsFalse(Base58Check.IsValid("1".AsSpan()));
    }

    /// <summary>
    /// Verifies that an empty payload round-trips — the encoded form is the Base58 of the 4-byte checksum alone.
    /// </summary>
    [TestMethod]
    public void RoundTrip_ForEmptyPayload_ShouldRecoverEmptyArray()
    {
        var encoded = Base58Check.Encode(ReadOnlySpan<byte>.Empty);
        var decoded = Base58Check.Decode(encoded.AsSpan());

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.Encode(ReadOnlySpan{byte}, Base58Variant)" /> followed by
    /// <see cref="Base58Check.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> recovers arbitrary input
    /// bytes across a sweep of payload sizes.
    /// </summary>
    /// <param name="payloadLength">The payload byte count under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(20)]
    [DataRow(32)]
    [DataRow(64)]
    public void RoundTrip_ShouldRecoverPayload(int payloadLength)
    {
        var payload = new byte[payloadLength];
        new Random(0xCAFE).NextBytes(payload);

        var encoded = Base58Check.Encode(payload);
        var decoded = Base58Check.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> succeeds for the canonical Genesis address and writes the
    /// expected 21-byte payload.
    /// </summary>
    [TestMethod]
    public void TryDecode_ForGenesisAddress_ShouldRecoverPayload()
    {
        var destination = new byte[GenesisPayload.Length];

        var ok = Base58Check.TryDecode(GenesisAddress.AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(GenesisPayload.Length, bytesWritten);
        CollectionAssert.AreEqual(GenesisPayload, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> returns <see langword="false" /> for a tampered input rather
    /// than throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenChecksumIsCorrupted_ShouldReturnFalse()
    {
        var tampered = GenesisAddress.ToCharArray();
        tampered[^1] = tampered[^1] == 'a' ? 'b' : 'a';
        var destination = new byte[GenesisPayload.Length];

        var ok = Base58Check.TryDecode(new string(tampered).AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

}
