// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for Base16 (hexadecimal) encoding, sourced from RFC 4648 §10.
/// </summary>
public static class Base16KnownAnswerVectors
{

    /// <summary>
    /// Returns malformed Base16 inputs that the strict decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> NegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Odd length single character", "a", typeof(FormatException), "RFC 4648 §6 length rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Odd length three characters", "abc", typeof(FormatException), "RFC 4648 §6 length rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Non-hex character 'g'", "ag", typeof(FormatException), "RFC 4648 §8 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Non-hex character 'Z'", "abZ0", typeof(FormatException), "RFC 4648 §8 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("0x prefix without AllowPrefix style", "0x1234", typeof(FormatException), "strict mode") };
        yield return new object[] { new EncodingNegativeDecodeVector("Whitespace without IgnoreWhitespace style", "12 34", typeof(FormatException), "strict mode") };
        yield return new object[] { new EncodingNegativeDecodeVector("Embedded non-ASCII letter", "abce¿12", typeof(FormatException), "RFC 4648 §8 alphabet") };
    }
    /// <summary>
    /// Returns the canonical RFC 4648 §10 Base16 test vectors. The reference outputs use upper case, which is the
    /// canonical Base16 case per RFC 4648 §8.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Rfc4648Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'f'", "f", "66", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fo'", "fo", "666F", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foo'", "foo", "666F6F", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foob'", "foob", "666F6F62", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fooba'", "fooba", "666F6F6261", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foobar'", "foobar", "666F6F626172", "RFC 4648 §10") };
    }

}
