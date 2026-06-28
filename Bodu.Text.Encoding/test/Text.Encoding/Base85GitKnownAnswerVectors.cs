// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitKnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Known Answer Test vectors for the Git-style Base85 variant (<see cref="Base85Variant.GitCompact" />). The compact
/// vectors are generated with the reference Python <c>base64.b85encode(..., pad=False)</c> using the Git alphabet, and
/// the two pinned boundary vectors (<c>00 00 00 00</c> → <c>00000</c>, <c>FF FF FF FF</c> → <c>|NsC0</c>) match Git's
/// <c>base85.c</c>.
/// </summary>
public static class Base85GitKnownAnswerVectors
{
    /// <summary>
    /// Returns Git compact-mode vectors covering full groups, partial tails, and the alphabet boundaries.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> GitCompactVectors()
    {
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Empty input", string.Empty, string.Empty, "Git base85.c") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four zero bytes (no shortcut)", "00000000", "00000", "Git base85.c") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four 0xFF bytes (highest 32-bit value)", "FFFFFFFF", "|NsC0", "Git base85.c") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("One byte (partial group → 2 chars)", "01", "0R", "Python b85encode pad=False") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Two bytes (partial group → 3 chars)", "0102", "0Rj", "Python b85encode pad=False") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Three bytes (partial group → 4 chars)", "010203", "0RjU", "Python b85encode pad=False") };
        yield return new object[] { EncodingKnownAnswerVector.FromHex("Four bytes (full group → 5 chars)", "01020304", "0RjUA", "Python b85encode pad=False") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'hello' (5 bytes)", "hello", "Xk~0{Zv", "Python b85encode pad=False") };
        yield return new object[] { EncodingKnownAnswerVector.FromAscii("ASCII 'hello world' (11 bytes)", "hello world", "Xk~0{Zy<MXa%^M", "Python b85encode pad=False") };
    }

    /// <summary>
    /// Returns malformed Git compact-mode inputs that the decoder must reject.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> GitNegativeVectors()
    {
        yield return new object[] { new EncodingNegativeDecodeVector("Single trailing character (length mod 5 == 1)", "0", typeof(FormatException), "Git compact final-group rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Six characters (full group + 1 trailing)", "0RjUA0", typeof(FormatException), "Git compact final-group rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Space not in Git alphabet", "0R j", typeof(FormatException), "Git alphabet") };
        yield return new object[] { new EncodingNegativeDecodeVector("Adobe 'z' shortcut rejected (treated as digit, mod 5 == 1)", "z", typeof(FormatException), "Git compact final-group rule") };
        yield return new object[] { new EncodingNegativeDecodeVector("Double-quote not in Git alphabet", "0R\"j", typeof(FormatException), "Git alphabet") };
    }
}
