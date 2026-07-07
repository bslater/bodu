// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Sha3ShortMsgKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the NIST SHA-3 competition <c>ShortMsgKAT</c> / <c>LongMsgKAT</c> file format — repeated
/// <c>Len = / Msg = / MD =</c> records where <c>Len</c> is the message length in <b>bits</b> — into known-answer
/// records. Only byte-aligned records (<c>Len</c> a multiple of 8) are emitted, since the algorithms under test operate
/// on whole bytes; bit-partial records are skipped.
/// </summary>
public static class Sha3ShortMsgKatReader
{
    /// <summary>
    /// Reads the byte-aligned records from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> each.
    /// </summary>
    /// <param name="stream">A readable stream over the ShortMsgKAT text.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The byte-aligned vectors, in source order.</returns>
    /// <exception cref="FormatException">A record is missing a field or a value is malformed.</exception>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, string? source = null)
    {
        using var reader = new StreamReader(stream);

        int? lenBits = null;
        string? msg = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;

            int eq = trimmed.IndexOf('=');
            if (eq < 0) continue;

            string label = trimmed[..eq].TrimEnd();
            string value = trimmed[(eq + 1)..].Trim();

            switch (label)
            {
                case "Len":
                    lenBits = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case "Msg":
                    msg = value;
                    break;

                case "MD":
                    if (lenBits is null || msg is null)
                        throw new FormatException("SHA-3 KAT record has an MD without a preceding Len/Msg.");

                    if (lenBits.Value % 8 == 0)
                    {
                        int byteLen = lenBits.Value / 8;
                        byte[] full = msg.Length == 0 ? [] : Convert.FromHexString(msg);
                        byte[] message = byteLen == 0 ? [] : full.AsSpan(0, byteLen).ToArray();

                        yield return new MessageDigestKnownAnswer
                        {
                            Name = source is null ? $"Len {lenBits.Value}" : $"{source} Len {lenBits.Value}",
                            Message = message,
                            Digest = Convert.FromHexString(value),
                        };
                    }

                    lenBits = null;
                    msg = null;
                    break;

                default:
                    break;
            }
        }
    }
}
