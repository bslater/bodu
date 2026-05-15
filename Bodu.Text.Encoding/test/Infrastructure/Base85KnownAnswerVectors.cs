// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85KnownAnswerVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for Base85 encoding. Ascii85 vectors are sourced from the Adobe Tech Note
/// 5045 examples (notably the Wikipedia-documented "Man " opening of the Leviathan quote). Z85 vectors are
/// sourced from RFC 32 (ZeroMQ).
/// </summary>
public static class Base85KnownAnswerVectors
{

    /// <summary>
    /// Returns malformed Ascii85 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Ascii85NegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Character above alphabet upper bound ('v' = 0x76)", "9jqov", typeof(FormatException), "Adobe Ascii85 alphabet '!'..'u'") };
        yield return new object[] { new EncodingNegativeDecodeVector("Control character below alphabet lower bound", "\x01\x02\x03\x04\x05", typeof(FormatException), "Adobe Ascii85 alphabet '!'..'u'") };
        yield return new object[] { new EncodingNegativeDecodeVector("Misplaced 'z' shortcut (not at group boundary)", "9jz", typeof(FormatException), "Adobe Ascii85 shortcut rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Single trailing character (partial group of 1)", "9", typeof(FormatException), "Adobe Ascii85 final-group rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Six characters (full group + 1 trailing)", "uuuuuu", typeof(FormatException), "Adobe Ascii85 final-group rule") };
    }
    /// <summary>
    /// Returns Ascii85 vectors that exercise the canonical alphabet and the <c>z</c> shortcut.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Ascii85Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Empty input", string.Empty, string.Empty, "Adobe Ascii85") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four zero bytes (z shortcut)", "00000000", "z", "Adobe Ascii85 §3.3") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Wikipedia 'Man ' (Leviathan opener)", "4D616E20", "9jqo^", "Wikipedia / Adobe Tech Note 5045") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Eight zero bytes (two z shortcuts)", "0000000000000000", "zz", "Adobe Ascii85 §3.3") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Single 0x00 byte (partial group)", "00", "!!", "Adobe Ascii85 partial-group rule") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Two 0x00 bytes (partial group)", "0000", "!!!", "Adobe Ascii85 partial-group rule") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Three 0x00 bytes (partial group)", "000000", "!!!!", "Adobe Ascii85 partial-group rule") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four 0xFF bytes (highest 32-bit value)", "FFFFFFFF", "s8W-!", "Adobe Ascii85 boundary case") };
    }

    /// <summary>
    /// Returns malformed Z85 inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Z85NegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Non-multiple-of-5 length", "HelloW", typeof(FormatException), "RFC 32 length rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Double-quote not in Z85 alphabet", "He\"loW", typeof(FormatException), "RFC 32 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Backslash not in Z85 alphabet", "He\\loW", typeof(FormatException), "RFC 32 alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Semicolon not in Z85 alphabet", "He;loW", typeof(FormatException), "RFC 32 alphabet") };
    }

    /// <summary>
    /// Returns Z85 vectors. The 8-byte sample is the canonical RFC 32 test vector.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Z85Vectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Empty input", string.Empty, string.Empty, "RFC 32 (ZeroMQ Z85)") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("RFC 32 reference vector ('HelloWorld')", "864FD26FB559F75B", "HelloWorld", "RFC 32 (ZeroMQ Z85)") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four zero bytes (Z85 has no shortcut)", "00000000", "00000", "RFC 32 (ZeroMQ Z85)") };
    }

}
