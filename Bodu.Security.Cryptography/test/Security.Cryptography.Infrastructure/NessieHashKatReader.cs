// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NessieHashKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses Project NESSIE hash test-vector files (the <c>message=… / hash=…</c> format used for Tiger, Whirlpool, and
/// related primitives) into known-answer records. The reader decodes the NESSIE message descriptions it can express as
/// byte inputs — quoted literals, <c>N times "X"</c> repetitions, byte-aligned <c>N zero bits</c> strings, and
/// <c>M-bit string: pattern</c> patterns — and skips the ones that are not a plain byte message (bit-unaligned zero-bit
/// strings and the Set-4 <c>iterated</c> Monte-Carlo rows).
/// </summary>
public static partial class NessieHashKatReader
{
    /// <summary>
    /// Reads NESSIE hash vectors from <paramref name="stream" />, yielding one <see cref="MessageDigestKnownAnswer" />
    /// per decodable record.
    /// </summary>
    /// <param name="stream">A readable stream over the NESSIE test-vector text.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The decodable vectors, in source order. Undecodable descriptions are skipped.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, string? source = null)
    {
        using var reader = new StreamReader(stream);

        string? setLabel = null;
        int index = 0;
        string? message = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            Match setMatch = SetHeader().Match(trimmed);
            if (setMatch.Success)
            {
                setLabel = setMatch.Groups[1].Value;
                index = int.Parse(setMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                message = null;
                continue;
            }

            if (trimmed.StartsWith("message=", StringComparison.Ordinal))
            {
                message = trimmed["message=".Length..].Trim();
                continue;
            }

            if (trimmed.StartsWith("hash=", StringComparison.Ordinal) && message is not null)
            {
                byte[]? decoded = DecodeMessage(message);
                if (decoded is not null)
                {
                    string label = source is null
                        ? $"Set {setLabel} #{index}"
                        : $"{source} Set {setLabel} #{index}";

                    yield return new MessageDigestKnownAnswer
                    {
                        Name = label,
                        Message = decoded,
                        Digest = Convert.FromHexString(trimmed["hash=".Length..].Trim()),
                    };
                }

                message = null;
            }
        }
    }

    /// <summary>
    /// Decodes a NESSIE message description into its byte input, or returns <see langword="null" /> when the
    /// description is not a plain byte message this reader models.
    /// </summary>
    /// <param name="description">The text following <c>message=</c>.</param>
    /// <returns>The decoded message bytes, or <see langword="null" /> to skip the row.</returns>
    private static byte[]? DecodeMessage(string description)
    {
        // Strip a trailing parenthetical annotation such as ' (empty string)'.
        int paren = description.IndexOf(" (", StringComparison.Ordinal);
        string desc = (paren >= 0 ? description[..paren] : description).Trim();

        // Special canonical description used by NESSIE Set 1: the printable-ASCII run A..Z a..z 0..9.
        if (desc is "\"A...Za...z0...9\"")
        {
            var sb = new StringBuilder();
            for (char c = 'A'; c <= 'Z'; c++) sb.Append(c);
            for (char c = 'a'; c <= 'z'; c++) sb.Append(c);
            for (char c = '0'; c <= '9'; c++) sb.Append(c);
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        // N times "X"  (e.g. 8 times "1234567890", 1 million times "a").
        Match times = TimesRepeat().Match(desc);
        if (times.Success)
        {
            long count = times.Groups[1].Value == "1 million"
                ? 1_000_000
                : long.Parse(times.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            byte[] unit = Encoding.ASCII.GetBytes(times.Groups[2].Value);
            byte[] buffer = new byte[checked((int)(count * unit.Length))];
            for (int i = 0; i < buffer.Length; i += unit.Length) unit.CopyTo(buffer, i);
            return buffer;
        }

        // A plain quoted literal.
        if (desc.Length >= 2 && desc[0] == '"' && desc[^1] == '"')
            return Encoding.ASCII.GetBytes(desc[1..^1]);

        // N zero bits — only byte-aligned lengths map onto a byte message.
        Match zeroBits = ZeroBits().Match(desc);
        if (zeroBits.Success)
        {
            int bits = int.Parse(zeroBits.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return bits % 8 == 0 ? new byte[bits / 8] : null;
        }

        // M-bit string: k1*HH,HH,k2*HH,...  (e.g. 512-bit string: 63*00,08, 0*00).
        Match bitString = BitStringPattern().Match(desc);
        if (bitString.Success)
            return DecodePattern(bitString.Groups[1].Value);

        // Anything else (Set-4 iterated rows and unrecognised forms) is skipped.
        return null;
    }

    /// <summary>
    /// Expands a NESSIE byte-pattern such as <c>63*00,08, 0*00</c> — comma-separated <c>count*HH</c> runs or single
    /// <c>HH</c> bytes — into the byte sequence it denotes.
    /// </summary>
    /// <param name="pattern">The comma-separated pattern text.</param>
    /// <returns>The expanded bytes.</returns>
    private static byte[] DecodePattern(string pattern)
    {
        var bytes = new List<byte>();
        foreach (string rawToken in pattern.Split(','))
        {
            string token = rawToken.Trim();
            if (token.Length == 0) continue;

            int star = token.IndexOf('*');
            if (star >= 0)
            {
                int count = int.Parse(token[..star], System.Globalization.CultureInfo.InvariantCulture);
                byte value = Convert.ToByte(token[(star + 1)..], 16);
                for (int i = 0; i < count; i++) bytes.Add(value);
            }
            else
            {
                bytes.Add(Convert.ToByte(token, 16));
            }
        }

        return [.. bytes];
    }

    /// <summary>
    /// Matches a NESSIE record header such as <c>Set 1, vector#  0:</c>.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^Set (\d+), vector#\s*(\d+):")]
    private static partial Regex SetHeader();

    /// <summary>
    /// Matches an <c>N times "X"</c> repetition description.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("^(1 million|\\d+) times \"(.*)\"$")]
    private static partial Regex TimesRepeat();

    /// <summary>
    /// Matches an <c>N zero bits</c> description.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^(\d+) zero bits$")]
    private static partial Regex ZeroBits();

    /// <summary>
    /// Matches an <c>M-bit string: pattern</c> description and captures the pattern.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^\d+-bit string:\s*(.*)$")]
    private static partial Regex BitStringPattern();
}
