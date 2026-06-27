// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableKnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for MIME Quoted-Printable (RFC 2045 §6.7). The positive vectors are expressed in
/// the default binary mode and pin the normative escape rules — uppercase <c>=HH</c> hex, the mandatory <c>=3D</c>
/// escape of the equals sign, and the protection of trailing whitespace — drawn from the RFC 2045 §6.7 worked examples.
/// </summary>
public static class QuotedPrintableKnownAnswerVectors
{
    /// <summary>
    /// Returns binary-mode vectors covering literal pass-through, the mandatory and example escapes, and trailing
    /// whitespace protection.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> BinaryVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Empty input", string.Empty, string.Empty, "RFC 2045 §6.7") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("Printable ASCII passes through", "Hello, World!", "Hello, World!", "RFC 2045 §6.7 rule 2") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Equals sign is always escaped", "3D", "=3D", "RFC 2045 §6.7 rule 1") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Form feed escapes with uppercase hex", "0C", "=0C", "RFC 2045 §6.7 rule 1") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Latin-1 'é' (0xE9) escapes as =E9", "E9", "=E9", "RFC 2045 §6.7 example") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Highest byte 0xFF escapes as =FF", "FF", "=FF", "RFC 2045 §6.7 rule 1") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Trailing space is escaped (=20)", "6120", "a=20", "RFC 2045 §6.7 rule 3") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Trailing tab is escaped (=09)", "6109", "a=09", "RFC 2045 §6.7 rule 3") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Interior space stays literal", "612062", "a b", "RFC 2045 §6.7 rule 3") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("CR and LF escape in binary mode", "0D0A", "=0D=0A", "RFC 2045 §6.7 rule 1") };
    }

    /// <summary>
    /// Returns malformed Quoted-Printable inputs that the strict decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> NegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Bare '=' at end of input", "=", typeof(FormatException), "RFC 2045 §6.7 rule 1") };
        yield return new object[] { new EncodingNegativeDecodeVector("'=' followed by a single character", "=A", typeof(FormatException), "RFC 2045 §6.7 rule 1") };
        yield return new object[] { new EncodingNegativeDecodeVector("'=' followed by non-hex digits", "=GG", typeof(FormatException), "RFC 2045 §6.7 rule 1") };
        yield return new object[] { new EncodingNegativeDecodeVector("'=' CR not followed by LF", "=\rX", typeof(FormatException), "RFC 2045 §6.7 rule 5") };
        yield return new object[] { new EncodingNegativeDecodeVector("Bare LF without AllowBareLineFeed", "=\n", typeof(FormatException), "RFC 2045 §6.7 rule 4") };
        yield return new object[] { new EncodingNegativeDecodeVector("Lone CR not followed by LF", "a\rb", typeof(FormatException), "RFC 2045 §6.7 rule 4") };
        yield return new object[] { new EncodingNegativeDecodeVector("Non-ASCII character", "é", typeof(FormatException), "RFC 2045 §6.7") };
        yield return new object[] { new EncodingNegativeDecodeVector("Lowercase hex escape (strict)", "=3d", typeof(FormatException), "RFC 2045 §6.7 rule 1") };
    }
}
