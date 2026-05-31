// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for Base64 encodings, sourced from RFC 4648 §10 (Standard alphabet) and the
/// RFC 4648 §5 URL- and filename-safe alphabet.
/// </summary>
public static class Base64KnownAnswerVectors
{

    /// <summary>
    /// Returns malformed Standard Base64 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> StandardNegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Invalid character '@'", "Zm@v", typeof(FormatException), "RFC 4648 §4 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("URL-safe '-' not in Standard alphabet", "Zm-v", typeof(FormatException), "RFC 4648 §4 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("URL-safe '_' not in Standard alphabet", "Zm_v", typeof(FormatException), "RFC 4648 §4 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Padding character in middle", "Z=m9", typeof(FormatException), "RFC 4648 §4 padding") };
        yield return new object[] { new EncodingNegativeDecodeVector("Missing required padding", "Zm9vYg", typeof(FormatException), "RFC 4648 §4 padding") };
        yield return new object[] { new EncodingNegativeDecodeVector("Only padding characters", "====", typeof(FormatException), "RFC 4648 §4 padding") };
    }
    /// <summary>
    /// Returns the canonical RFC 4648 §10 vectors for the Standard (§4) Base64 alphabet.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> StandardRfc4648Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'f'", "f", "Zg==", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fo'", "fo", "Zm8=", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foo'", "foo", "Zm9v", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foob'", "foob", "Zm9vYg==", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fooba'", "fooba", "Zm9vYmE=", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foobar'", "foobar", "Zm9vYmFy", "RFC 4648 §10") };
    }

    /// <summary>
    /// Returns malformed URL-safe Base64 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> UrlSafeNegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Standard '+' not in URL-safe alphabet", "Zm+v", typeof(FormatException), "RFC 4648 §5 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Standard '/' not in URL-safe alphabet", "Zm/v", typeof(FormatException), "RFC 4648 §5 alphabet") };
    }

    /// <summary>
    /// Returns URL-safe Base64 vectors. The inputs are chosen so the output differs from the Standard alphabet
    /// (i.e. produces at least one <c>+</c>/<c>/</c> in Standard which becomes <c>-</c>/<c>_</c> in URL-safe).
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> UrlSafeVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Three 0xFB/FF/FE bytes (produces +//+ in Standard)", "FBFFFE", "-__-", "RFC 4648 §5") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four 0xFF bytes (boundary value)", "FFFFFFFF", "_____w", "RFC 4648 §5") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foobar' (no alphabet difference)", "foobar", "Zm9vYmFy", "RFC 4648 §5") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Empty input", string.Empty, string.Empty, "RFC 4648 §5") };
    }

}
