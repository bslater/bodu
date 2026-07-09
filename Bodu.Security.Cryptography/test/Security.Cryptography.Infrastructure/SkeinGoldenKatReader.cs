// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinGoldenKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses Skein 1.3 <c>skein_golden_kat</c> reference files into hash known-answer records. Each record is introduced
/// by a header such as <c>:Skein-256:   256-bit hash, msgLen =   256 bits</c>, followed by a <c>Message data:</c> hex
/// block and a <c>Result:</c> hex block, and terminated by a dashed separator. Only plain hash records (no key / MAC /
/// tree) are recognised; other record kinds are skipped.
/// </summary>
public static partial class SkeinGoldenKatReader
{
    /// <summary>
    /// Reads the hash known-answer vectors from <paramref name="stream" />, yielding one
    /// <see cref="SkeinKnownAnswer" /> per plain-hash record.
    /// </summary>
    /// <param name="stream">A readable stream over the Skein golden-KAT text.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The parsed hash vectors, in source order.</returns>
    public static IEnumerable<SkeinKnownAnswer> Read(Stream stream, string? source = null)
    {
        using var reader = new StreamReader(stream);

        int state = 0, output = 0, msgLenBits = 0;
        bool active = false;
        int mode = 0; // 0=none, 1=message, 2=result
        var msg = new System.Text.StringBuilder();
        var res = new System.Text.StringBuilder();

        SkeinKnownAnswer? Emit()
        {
            if (!active || res.Length == 0) return null;
            byte[] message = msgLenBits == 0 || msg.Length == 0 ? [] : Convert.FromHexString(msg.ToString());
            return new SkeinKnownAnswer
            {
                Name = source is null
                    ? $"Skein-{state} {output}-bit, msg {msgLenBits} bits"
                    : $"{source} Skein-{state} {output}-bit, msg {msgLenBits} bits",
                StateSizeBits = state,
                OutputBits = output,
                Message = message,
                Digest = Convert.FromHexString(res.ToString()),
            };
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            Match header = HashHeader().Match(trimmed);
            if (header.Success)
            {
                state = int.Parse(header.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                output = int.Parse(header.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                msgLenBits = int.Parse(header.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                active = true;
                mode = 0;
                msg.Clear();
                res.Clear();
                continue;
            }

            // Any other ':Skein-…:' header (MAC, tree, …) that is not a plain hash ends the current record and is skipped.
            if (trimmed.StartsWith(":Skein", StringComparison.Ordinal))
            {
                SkeinKnownAnswer? pending = Emit();
                if (pending is not null) yield return pending;
                active = false;
                mode = 0;
                continue;
            }

            if (trimmed.StartsWith("Message data", StringComparison.Ordinal)) { mode = 1; continue; }
            if (trimmed.StartsWith("Result", StringComparison.Ordinal)) { mode = 2; continue; }

            if (trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                SkeinKnownAnswer? pending = Emit();
                if (pending is not null) yield return pending;
                active = false;
                mode = 0;
                continue;
            }

            if (active && mode != 0 && HexLine().IsMatch(trimmed))
            {
                string hex = trimmed.Replace(" ", string.Empty, StringComparison.Ordinal);
                if (mode == 1) msg.Append(hex);
                else res.Append(hex);
            }
        }

        SkeinKnownAnswer? tail = Emit();
        if (tail is not null) yield return tail;
    }

    /// <summary>
    /// Matches a plain sequential-hash record header and captures state size, output size, and message length in bits.
    /// The trailing <c>$</c> anchor excludes tree-hashing headers (which append <c>. Tree: …</c>) and any MAC/keyed
    /// variants, so only the sequential-hash records Bodu's <c>ComputeHash</c> models are emitted.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^:Skein-(\d+):\s+(\d+)-bit hash, msgLen\s*=\s*(\d+) bits\s*$")]
    private static partial Regex HashHeader();

    /// <summary>
    /// Matches a line of space-separated hex byte groups.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("^[0-9A-Fa-f ]+$")]
    private static partial Regex HexLine();
}
