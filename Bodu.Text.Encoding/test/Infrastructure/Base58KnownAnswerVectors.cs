// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58KnownAnswerVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for Base58 encoding, sourced from the Bitcoin Core reference test fixture
/// <c>base58_encode_decode.json</c>.
/// </summary>
/// <remarks>
/// These vectors are the canonical Base58 KAT used by the Bitcoin reference implementation and many derivative
/// projects (IPFS, Solana, etc.). The decoded form is expressed as hex per the original JSON fixture.
/// </remarks>
public static class Base58KnownAnswerVectors
{

    /// <summary>
    /// Returns malformed Bitcoin/Flickr Base58 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> BitcoinFlickrNegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Visually ambiguous '0' (zero) excluded", "0Ajdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Visually ambiguous 'O' (capital O) excluded", "OAjdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Visually ambiguous 'I' (capital I) excluded", "IAjdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Visually ambiguous 'l' (lower L) excluded", "lAjdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Whitespace without IgnoreWhitespace style", "9Aj dvzr", typeof(FormatException), "strict mode") };
        yield return new object[] { new EncodingNegativeDecodeVector("Special character '!'", "!Ajdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Special character '@'", "@Ajdvzr", typeof(FormatException), "Bitcoin/Flickr alphabet") };
    }
    /// <summary>
    /// Returns the Bitcoin Core <c>base58_encode_decode.json</c> Bitcoin/Flickr alphabet vectors.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> BitcoinFlickrVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Empty input", string.Empty, string.Empty, "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Single byte 0x61", "61", "2g", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Three 0x62 bytes", "626262", "a3gV", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Three 0x63 bytes", "636363", "aPEr", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("ASCII 'simply a long string'", "73696d706c792061206c6f6e6720737472696e67", "2cFupjhnEsSn59qHXstmK2ffpLv2", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Bitcoin mainnet P2PKH address payload", "00eb15231dfceb60925886b67d065299925915aeb172c06647", "1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Five-byte 0x516B6FCD0F", "516b6fcd0f", "ABnLTmg", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Nine-byte 0xBF4F89001E670274DD", "bf4f89001e670274dd", "3SEo3LWLoPntC", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four-byte 0x572E4794", "572e4794", "3EFU7m", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Ten-byte 0xECAC89CAD93923C02321", "ecac89cad93923c02321", "EJDM8drfXA6uyA", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four-byte 0x10C8511E", "10c8511e", "Rt5zm", "Bitcoin Core base58_encode_decode.json") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Ten zero bytes (all leading-zero markers)", "00000000000000000000", "1111111111", "Bitcoin Core base58_encode_decode.json") };
    }

}
