// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hc128SpecKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the HC-128 specification Appendix A keystream vectors (Hongjun Wu, <c>The Stream Cipher HC-128</c>) into
/// <see cref="StreamCipherKnownAnswer" /> rows. Each numbered block declares its key / IV setup in prose (all-zero by
/// default, overridden by explicit <c>Kn = value</c> / <c>IVn = value</c> word assignments) followed by sixteen 32-bit
/// keystream words. HC-128 emits each output word least-significant-byte first, so the reader serializes every word
/// little-endian to recover the byte keystream.
/// </summary>
public static partial class Hc128SpecKatReader
{
    /// <summary>
    /// Reads every Appendix A keystream vector from <paramref name="stream" />, yielding one
    /// <see cref="StreamCipherKnownAnswer" /> per numbered block.
    /// </summary>
    /// <param name="stream">A readable stream over the HC-128 Appendix A source.</param>
    /// <returns>The vectors, in source order.</returns>
    public static IEnumerable<StreamCipherKnownAnswer> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        int? number = null;
        string setup = string.Empty;
        byte[] key = new byte[16];
        byte[] iv = new byte[16];
        var words = new List<uint>(16);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            Match header = HeaderPattern().Match(trimmed);
            if (header.Success)
            {
                if (number is not null && words.Count > 0)
                    yield return Build(number.Value, setup, key, iv, words);

                number = int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture);
                setup = header.Groups[2].Value.Trim();
                key = new byte[16];
                iv = new byte[16];
                words = [];
                ApplyAssignments(setup, key, iv);
                continue;
            }

            if (number is null)
                continue;

            foreach (string token in trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (WordPattern().IsMatch(token))
                    words.Add(uint.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
        }

        if (number is not null && words.Count > 0)
            yield return Build(number.Value, setup, key, iv, words);
    }

    /// <summary>
    /// Applies each <c>Kn = value</c> / <c>IVn = value</c> word override found in <paramref name="setup" />.
    /// </summary>
    /// <param name="setup">The block's prose setup description.</param>
    /// <param name="key">The 16-byte key buffer to mutate.</param>
    /// <param name="iv">The 16-byte IV buffer to mutate.</param>
    private static void ApplyAssignments(string setup, byte[] key, byte[] iv)
    {
        foreach (Match assignment in AssignmentPattern().Matches(setup))
        {
            bool isIv = assignment.Groups[1].Value.Equals("IV", StringComparison.OrdinalIgnoreCase);
            int wordIndex = int.Parse(assignment.Groups[2].Value, CultureInfo.InvariantCulture);
            string raw = assignment.Groups[3].Value;
            uint value = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.Parse(raw[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : uint.Parse(raw, CultureInfo.InvariantCulture);

            BinaryPrimitives.WriteUInt32LittleEndian((isIv ? iv : key).AsSpan(wordIndex * 4, 4), value);
        }
    }

    /// <summary>
    /// Serializes the collected words little-endian and packages them as a keystream vector.
    /// </summary>
    /// <param name="number">The Appendix A block number.</param>
    /// <param name="setup">The block's prose setup description.</param>
    /// <param name="key">The resolved key bytes.</param>
    /// <param name="iv">The resolved IV bytes.</param>
    /// <param name="words">The sixteen keystream words.</param>
    /// <returns>The constructed keystream known-answer vector.</returns>
    private static StreamCipherKnownAnswer Build(int number, string setup, byte[] key, byte[] iv, List<uint> words)
    {
        byte[] keystream = new byte[words.Count * 4];
        for (int i = 0; i < words.Count; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(keystream.AsSpan(i * 4, 4), words[i]);

        return new StreamCipherKnownAnswer
        {
            Name = $"HC-128 Appendix A.{number} ({setup})",
            Provenance = KatProvenance.ReferenceImplementation($"Hongjun Wu, The Stream Cipher HC-128, Appendix A.{number}"),
            Key = key,
            Nonce = iv,
            IsKeystream = true,
            Plaintext = [],
            Ciphertext = keystream,
        };
    }

    [GeneratedRegex(@"^(\d+)\.\s+(.*)$")]
    private static partial Regex HeaderPattern();

    [GeneratedRegex(@"^[0-9a-fA-F]{8}$")]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"\b(IV|K)\s*(\d+)\s*=\s*(0x[0-9A-Fa-f]+|\d+)")]
    private static partial Regex AssignmentPattern();
}
