// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffContinuedText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Biff;

/// <summary>
/// Assembles the characters of a BIFF8 string whose characters continue across records, one segment at a time, so that
/// they are decoded as a single run.
/// </summary>
/// <remarks>
/// A <c>CONTINUE</c> boundary may fall between any two UTF-16 code units of a string, the two code units of a surrogate
/// pair included. Decoding each segment on its own would turn both halves of such a pair into U+FFFD, so the code units
/// of every segment are copied as they are, and only once the whole string is assembled is a surrogate without its
/// partner replaced with U+FFFD, as decoding the same code units in one record does.
/// </remarks>
internal static class BiffContinuedText
{
    /// <summary>The character that replaces a surrogate code unit without its partner.</summary>
    private const char ReplacementCharacter = '\uFFFD';

    /// <summary>
    /// Copies the little-endian 16-bit code units of a segment into the destination without decoding them.
    /// </summary>
    /// <param name="source">The segment's code units, two bytes each.</param>
    /// <param name="destination">The characters that receive the code units, one for each.</param>
    public static void CopyCodeUnits(ReadOnlySpan<byte> source, Span<char> destination)
    {
        for (int i = 0; i < destination.Length; i++)
            destination[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(source[(i * 2)..]);
    }

    /// <summary>
    /// Widens the 8-bit characters of a compressed segment, each the low byte of a code unit whose high byte is zero,
    /// into the destination.
    /// </summary>
    /// <param name="source">The segment's characters, one byte each.</param>
    /// <param name="destination">The characters that receive them, one for each.</param>
    public static void Widen(ReadOnlySpan<byte> source, Span<char> destination)
    {
        for (int i = 0; i < source.Length; i++)
            destination[i] = (char)source[i];
    }

    /// <summary>
    /// Replaces each surrogate code unit that is not half of a surrogate pair with U+FFFD, as decoding the same code
    /// units as UTF-16 in one run does.
    /// </summary>
    /// <param name="text">The assembled code units of the whole string, replaced in place.</param>
    public static void ReplaceUnpairedSurrogates(Span<char> text)
    {
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i += 2;
                continue;
            }

            if (char.IsSurrogate(text[i]))
                text[i] = ReplacementCharacter;

            i++;
        }
    }
}
