// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Bech32" /> (BIP-173) and Bech32m (BIP-350). Partial files split coverage by member
/// or subject contract per the repository test convention.
/// </summary>
[TestClass]
public sealed partial class Bech32Tests
{
    /// <summary>
    /// Verifies that encoding an empty data part under the canonical BIP-173 / BIP-350 human-readable parts reproduces
    /// the published checksum suffix for each scheme.
    /// </summary>
    /// <param name="hrp">The human-readable part.</param>
    /// <param name="encoding">The scheme under test.</param>
    /// <param name="expected">The published encoded string.</param>
    [TestMethod]
    [DataRow("a", Bech32Encoding.Bech32, "a12uel5l")]
    [DataRow("?", Bech32Encoding.Bech32, "?1ezyfcl")]
    [DataRow("a", Bech32Encoding.Bech32m, "a1lqfn3a")]
    [DataRow("?", Bech32Encoding.Bech32m, "?1v759aa")]
    public void Encode_WhenEmptyDataAndKnownHrp_ShouldReturnPublishedVector(string hrp, Bech32Encoding encoding, string expected)
    {
        Assert.AreEqual(expected, Bech32.Encode(hrp, ReadOnlySpan<byte>.Empty, encoding));
    }

    /// <summary>
    /// Verifies that decoding the canonical BIP-173 / BIP-350 vectors recovers the human-readable part, an empty data
    /// part, and the validating scheme.
    /// </summary>
    /// <param name="source">The encoded string.</param>
    /// <param name="expectedHrp">The expected lower-case human-readable part.</param>
    /// <param name="expectedEncoding">The expected validating scheme.</param>
    [TestMethod]
    [DataRow("a12uel5l", "a", Bech32Encoding.Bech32)]
    [DataRow("A12UEL5L", "a", Bech32Encoding.Bech32)]
    [DataRow("?1ezyfcl", "?", Bech32Encoding.Bech32)]
    [DataRow("a1lqfn3a", "a", Bech32Encoding.Bech32m)]
    [DataRow("?1v759aa", "?", Bech32Encoding.Bech32m)]
    public void Decode_WhenKnownVector_ShouldRecoverPartsAndScheme(string source, string expectedHrp, Bech32Encoding expectedEncoding)
    {
        Bech32.Decode(source, out var hrp, out var data, out var encoding);

        Assert.AreEqual(expectedHrp, hrp);
        Assert.AreEqual(0, data.Length);
        Assert.AreEqual(expectedEncoding, encoding);
    }

    /// <summary>
    /// Verifies that a long human-readable part containing an internal <c>1</c> is split on the final separator, per
    /// the BIP-173 worked example whose HRP embeds the digit one.
    /// </summary>
    [TestMethod]
    public void Decode_WhenHrpContainsInternalOne_ShouldSplitOnFinalSeparator()
    {
        const string Vector = "an83characterlonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio1tt5tgs";

        Bech32.Decode(Vector, out var hrp, out var data, out var encoding);

        Assert.AreEqual("an83characterlonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio", hrp);
        Assert.AreEqual(0, data.Length);
        Assert.AreEqual(Bech32Encoding.Bech32, encoding);
    }

    /// <summary>
    /// Verifies that the BIP-173 P2WPKH example address encodes from its witness version and program, and decodes back
    /// to the same parts. Witness version 0 uses the original Bech32 scheme.
    /// </summary>
    [TestMethod]
    public void EncodeDecode_WhenBip173SegWitV0Example_ShouldMatchPublishedAddress()
    {
        const string Address = "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";
        var program = Convert.FromHexString("751e76e8199196d454941c45d1b3a323f1433bd6");

        // Data part = witness version (0) followed by the program repacked from 8-bit to 5-bit groups.
        var programGroups = Bech32.ConvertBits(program, 8, 5, pad: true)!;
        var data = new byte[1 + programGroups.Length];
        programGroups.CopyTo(data, 1);

        var encoded = Bech32.Encode("bc", data, Bech32Encoding.Bech32);
        Assert.AreEqual(Address, encoded);

        Bech32.Decode(Address, out var hrp, out var decoded, out var encoding);
        Assert.AreEqual("bc", hrp);
        Assert.AreEqual(Bech32Encoding.Bech32, encoding);
        Assert.AreEqual(0, decoded[0]);
        CollectionAssert.AreEqual(program, Bech32.ConvertBits(decoded.AsSpan(1).ToArray(), 5, 8, pad: false));
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.EncodeFromBytes(string, ReadOnlySpan{byte}, Bech32Encoding)" /> and
    /// <see cref="Bech32.DecodeToBytes(string, out string, out byte[], out Bech32Encoding)" /> round-trip arbitrary
    /// byte payloads for both schemes.
    /// </summary>
    /// <param name="encoding">The scheme under test.</param>
    [TestMethod]
    [DataRow(Bech32Encoding.Bech32)]
    [DataRow(Bech32Encoding.Bech32m)]
    public void EncodeFromBytesDecodeToBytes_WhenRoundTripped_ShouldRecoverBytes(Bech32Encoding encoding)
    {
        byte[] payload = [0x00, 0x01, 0x02, 0xDE, 0xAD, 0xBE, 0xEF, 0xFF];

        var encoded = Bech32.EncodeFromBytes("bodu", payload, encoding);
        Bech32.DecodeToBytes(encoded, out var hrp, out var decoded, out var decodedEncoding);

        Assert.AreEqual("bodu", hrp);
        Assert.AreEqual(encoding, decodedEncoding);
        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that a Bech32 checksum does not validate when interpreted as Bech32m, confirming the two schemes use
    /// distinct constants.
    /// </summary>
    [TestMethod]
    public void Decode_WhenBech32StringDecoded_ShouldNotReportBech32m()
    {
        Bech32.Decode("a12uel5l", out _, out _, out var encoding);

        Assert.AreNotEqual(Bech32Encoding.Bech32m, encoding);
    }
}
