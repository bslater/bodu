// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OpenSslDigestKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the OpenSSL <c>evptests</c> message-digest reference format (blank-line-separated blocks of
/// <c>Key = Value</c> lines carrying <c>Digest</c>, <c>Input</c>, an optional <c>Count</c>, and <c>Output</c>) into
/// known-answer records. The <c>Input</c> field is a quoted ASCII literal; when a <c>Count</c> is present the literal
/// is repeated that many times (for example the ISO/IEC 10118-3 one-million-<c>a</c> vector). Only blocks whose
/// <c>Digest</c> matches the requested algorithm name are emitted.
/// </summary>
public static class OpenSslDigestKatReader
{
    /// <summary>
    /// Reads the vectors for <paramref name="digestName" /> from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> per matching block with the ASCII message reconstructed (and repeated
    /// per <c>Count</c>).
    /// </summary>
    /// <param name="stream">A readable stream over the OpenSSL evptests source.</param>
    /// <param name="digestName">The <c>Digest</c> value to select, for example <c>whirlpool</c>.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The matching vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, string digestName, string? source = null)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        string? digest = null;
        string? input = null;
        string? output = null;
        int count = 1;
        int index = 0;

        MessageDigestKnownAnswer? Flush()
        {
            if (digest is not null
                && digest.Equals(digestName, StringComparison.OrdinalIgnoreCase)
                && input is not null
                && output is not null)
            {
                byte[] unit = Encoding.ASCII.GetBytes(input);
                byte[] message = count == 1 ? unit : Repeat(unit, count);

                var vector = new MessageDigestKnownAnswer
                {
                    Name = source is null ? $"{digestName} #{index}" : $"{source} {digestName} #{index}",
                    Message = message,
                    Digest = Hex(output),
                };

                return vector;
            }

            return null;
        }

        foreach (string rawLine in ReadLines(reader))
        {
            string line = rawLine.Trim();

            if (line.Length == 0)
            {
                MessageDigestKnownAnswer? completed = Flush();
                if (completed is not null)
                {
                    yield return completed;
                    index++;
                }

                digest = input = output = null;
                count = 1;
                continue;
            }

            if (line.StartsWith('#'))
                continue;

            int equals = line.IndexOf('=');
            if (equals < 0)
                continue;

            string key = line[..equals].Trim();
            string value = line[(equals + 1)..].Trim();

            switch (key)
            {
                case "Digest":
                    digest = value;
                    break;
                case "Input":
                    input = Unquote(value);
                    break;
                case "Count":
                    count = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "Output":
                    output = value;
                    break;
                default:
                    break;
            }
        }

        // Emit a trailing block that is not terminated by a blank line.
        MessageDigestKnownAnswer? tail = Flush();
        if (tail is not null)
            yield return tail;
    }

    /// <summary>Enumerates the lines of <paramref name="reader" /> to completion.</summary>
    /// <param name="reader">The reader to drain.</param>
    /// <returns>Each source line, without its terminator.</returns>
    private static IEnumerable<string> ReadLines(StreamReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;
    }

    /// <summary>Strips a single pair of surrounding double quotes from <paramref name="value" /> when present.</summary>
    /// <param name="value">The raw field value.</param>
    /// <returns>The unquoted literal.</returns>
    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;

    /// <summary>Concatenates <paramref name="unit" /> with itself <paramref name="count" /> times.</summary>
    /// <param name="unit">The repeating unit.</param>
    /// <param name="count">The number of repetitions.</param>
    /// <returns>A new array holding <paramref name="count" /> copies of <paramref name="unit" />.</returns>
    private static byte[] Repeat(byte[] unit, int count)
    {
        byte[] result = new byte[unit.Length * count];
        for (int i = 0; i < count; i++)
            Buffer.BlockCopy(unit, 0, result, i * unit.Length, unit.Length);

        return result;
    }
}
