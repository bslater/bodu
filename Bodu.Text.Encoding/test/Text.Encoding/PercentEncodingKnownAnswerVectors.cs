// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingKnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for percent-encoding. The <see cref="PercentEncodingMode.UriComponent" /> vectors
/// are drawn from RFC 3986 §2.1–2.3 (unreserved set and percent-encoded octets); the
/// <see cref="PercentEncodingMode.FormUrlEncoded" /> vectors follow the WHATWG URL Standard
/// <c>application/x-www-form-urlencoded</c> serializer. Each supplier is paired with the mode the rows were authored
/// against.
/// </summary>
public static class PercentEncodingKnownAnswerVectors
{
    /// <summary>
    /// Returns <see cref="PercentEncodingMode.UriComponent" /> vectors exercising the unreserved set and the
    /// percent-encoding of reserved octets.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> UriComponentVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 3986 §2.1") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Unreserved set passes through", "AZaz09-._~", "AZaz09-._~", "RFC 3986 §2.3") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Space encodes as %20", "hello world", "hello%20world", "RFC 3986 §2.1") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Reserved gen/sub-delims encode", "a/b?c=d", "a%2Fb%3Fc%3Dd", "RFC 3986 §2.2") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Percent encodes as %25", "100%", "100%25", "RFC 3986 §2.1") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("UTF-8 '‽' (U+203D) octets", "E280BD", "%E2%80%BD", "RFC 3986 §2.5") };
    }

    /// <summary>
    /// Returns <see cref="PercentEncodingMode.FormUrlEncoded" /> vectors following the WHATWG serializer rules.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> FormUrlEncodedVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Space encodes as plus", "a b", "a+b", "WHATWG URL Standard") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Literal plus encodes as %2B", "a+b", "a%2Bb", "WHATWG URL Standard") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Percent encodes as %25", "50%", "50%25", "WHATWG URL Standard") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Tilde encodes as %7E", "~", "%7E", "WHATWG URL Standard") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Form pass-through set", "Az09*-._", "Az09*-._", "WHATWG URL Standard") };
    }

    /// <summary>
    /// Returns malformed percent-encoded inputs that the strict decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> NegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Bare percent", "%", typeof(FormatException), "RFC 3986 §2.1") };
        yield return new object[] { new EncodingNegativeDecodeVector("Percent plus one hex digit", "%A", typeof(FormatException), "RFC 3986 §2.1") };
        yield return new object[] { new EncodingNegativeDecodeVector("Percent plus non-hex digits", "%GG", typeof(FormatException), "RFC 3986 §2.1") };
        yield return new object[] { new EncodingNegativeDecodeVector("Obsolete %uXXXX escape", "%u1234", typeof(FormatException), "RFC 3986 (non-conformant)") };
        yield return new object[] { new EncodingNegativeDecodeVector("Non-ASCII source character", "é", typeof(FormatException), "RFC 3986 §2.1") };
    }
}
