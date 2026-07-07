// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaChaDraftVectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the XChaCha test vectors from <c>draft-arciszewski-xchacha-03</c>. The Section 2.2.1 HChaCha20 block-function
/// vector is expressed as single <c>Key = / Nonce = / Subkey =</c> hex lines; the Appendix A.3 "developer-friendly"
/// vectors are labelled blocks (<c>Key:</c>, <c>IV:</c>, <c>Plaintext:</c>, <c>Keystream:</c>, <c>Ciphertext:</c>, …)
/// whose values are indented, multi-line continuous hex.
/// </summary>
public static partial class XChaChaDraftVectorReader
{
    /// <summary>
    /// Reads the Section 2.2.1 HChaCha20 block-function vector.
    /// </summary>
    /// <param name="stream">A readable stream over the draft source.</param>
    /// <returns>The key, 16-byte nonce, and expected 32-byte subkey.</returns>
    public static (byte[] Key, byte[] Nonce, byte[] Subkey) ReadHChaCha20(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        byte[]? key = null;
        byte[]? nonce = null;
        byte[]? subkey = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            Match assignment = AssignmentPattern().Match(line);
            if (!assignment.Success)
                continue;

            byte[] value = Convert.FromHexString(assignment.Groups[2].Value);
            switch (assignment.Groups[1].Value)
            {
                case "Key": key = value; break;
                case "Nonce": nonce = value; break;
                case "Subkey": subkey = value; break;
                default: break;
            }

            if (key is not null && nonce is not null && subkey is not null)
                break;
        }

        return (key!, nonce!, subkey!);
    }

    /// <summary>
    /// Reads the labelled hex fields of the Appendix A.3 developer-friendly section whose heading starts with
    /// <paramref name="sectionTitle" />.
    /// </summary>
    /// <param name="stream">A readable stream over the draft source.</param>
    /// <param name="sectionTitle">The section heading prefix, for example <c>A.3.2.  XChaCha20</c>.</param>
    /// <returns>The section's fields and optional counter, packaged as an <see cref="Rfc8439TestVector" />.</returns>
    public static Rfc8439TestVector ReadDeveloperFriendly(Stream stream, string sectionTitle)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        bool inSection = false;
        uint? counter = null;
        string? label = null;
        var fields = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            if (!inSection)
            {
                if (trimmed.StartsWith(sectionTitle, StringComparison.OrdinalIgnoreCase))
                    inSection = true;
                continue;
            }

            if (SectionHeadingPattern().IsMatch(trimmed) && !trimmed.StartsWith(sectionTitle, StringComparison.OrdinalIgnoreCase))
                break;

            Match counterMatch = CounterPattern().Match(trimmed);
            if (counterMatch.Success)
            {
                counter = uint.Parse(counterMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                label = null;
                continue;
            }

            Match labelMatch = LabelPattern().Match(trimmed);
            if (labelMatch.Success)
            {
                label = labelMatch.Groups[1].Value.Trim();
                fields[label] = new StringBuilder();
                continue;
            }

            if (label is null || trimmed.Length == 0)
                continue;

            Match hex = HexLinePattern().Match(trimmed);
            if (hex.Success)
                fields[label].Append(hex.Groups[1].Value);
        }

        return new Rfc8439TestVector(
            0,
            fields.ToDictionary(pair => pair.Key, pair => Convert.FromHexString(pair.Value.ToString()), StringComparer.OrdinalIgnoreCase),
            counter);
    }

    [GeneratedRegex(@"^\s*(Key|Nonce|Subkey)\s*=\s*([0-9a-fA-F]+)\s*$")]
    private static partial Regex AssignmentPattern();

    [GeneratedRegex(@"^Counter:\s*(\d+)")]
    private static partial Regex CounterPattern();

    [GeneratedRegex(@"^([A-Za-z0-9][A-Za-z0-9 -]*):$")]
    private static partial Regex LabelPattern();

    [GeneratedRegex(@"^([0-9a-fA-F]+)$")]
    private static partial Regex HexLinePattern();

    [GeneratedRegex(@"^A\.\d+")]
    private static partial Regex SectionHeadingPattern();
}
