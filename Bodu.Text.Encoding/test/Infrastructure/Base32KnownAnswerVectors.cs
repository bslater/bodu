// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for Base32 encodings, sourced from RFC 4648 §10 (Standard alphabet),
/// RFC 4648 §10 (base32hex / HexExtended), and the Crockford Base32 specification.
/// </summary>
public static class Base32KnownAnswerVectors
{

    /// <summary>
    /// Returns the canonical RFC 4648 §10 vectors for the base32hex (§7) alphabet.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> HexExtendedRfc4648Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'f'", "f", "CO======", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fo'", "fo", "CPNG====", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foo'", "foo", "CPNMU===", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foob'", "foob", "CPNMUOG=", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fooba'", "fooba", "CPNMUOJ1", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foobar'", "foobar", "CPNMUOJ1E8======", "RFC 4648 §10") };
    }

    /// <summary>
    /// Returns malformed Standard Base32 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> StandardNegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Digit '0' not in Standard alphabet", "MZ0W6YTB", typeof(FormatException), "RFC 4648 §6 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Digit '1' not in Standard alphabet", "MZ1W6YTB", typeof(FormatException), "RFC 4648 §6 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Digit '8' not in Standard alphabet", "MZ8W6YTB", typeof(FormatException), "RFC 4648 §6 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Digit '9' not in Standard alphabet", "MZ9W6YTB", typeof(FormatException), "RFC 4648 §6 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Padding character in middle", "MZ=XW6YT", typeof(FormatException), "RFC 4648 §6 padding") };
        yield return new object[] { new EncodingNegativeDecodeVector("Missing required padding", "MZXW6YTBOI", typeof(FormatException), "RFC 4648 §6 padding") };
        yield return new object[] { new EncodingNegativeDecodeVector("Invalid terminal-quantum length (1 data char)", "M=======", typeof(FormatException), "RFC 4648 §6 quantum") };
        yield return new object[] { new EncodingNegativeDecodeVector("Invalid terminal-quantum length (3 data chars)", "MZX=====", typeof(FormatException), "RFC 4648 §6 quantum") };
        yield return new object[] { new EncodingNegativeDecodeVector("Invalid terminal-quantum length (6 data chars)", "MZXW6Y==", typeof(FormatException), "RFC 4648 §6 quantum") };
    }
    /// <summary>
    /// Returns the canonical RFC 4648 §10 vectors for the Standard (§6) Base32 alphabet.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> StandardRfc4648Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'f'", "f", "MY======", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fo'", "fo", "MZXQ====", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foo'", "foo", "MZXW6===", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foob'", "foob", "MZXW6YQ=", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'fooba'", "fooba", "MZXW6YTB", "RFC 4648 §10") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'foobar'", "foobar", "MZXW6YTBOI======", "RFC 4648 §10") };
    }

}
