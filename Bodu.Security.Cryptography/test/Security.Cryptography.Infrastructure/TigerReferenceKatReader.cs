// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerReferenceKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the labelled <c>m:</c>/<c>h:</c> Tiger reference vector format (Biham's Tiger test results plus the NESSIE
/// Tiger / Tiger2 sets), honouring the <c>m-format:</c> directive (<c>ascii</c> or <c>hex</c>) and the
/// <c>option: variant = tiger2</c> switch. The caller selects which padding variant to yield; rows for the other
/// variant, and message forms this reader does not model, are skipped.
/// </summary>
public static partial class TigerReferenceKatReader
{
    /// <summary>
    /// Reads Tiger reference vectors from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> per row that matches the requested padding variant.
    /// </summary>
    /// <param name="stream">A readable stream over the labelled Tiger reference text.</param>
    /// <param name="tiger2">
    /// <see langword="true" /> to yield the Tiger2 (0x80-padding) rows; <see langword="false" /> for the standard Tiger
    /// (0x01-padding) rows.
    /// </param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The matching vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, bool tiger2, string? source = null)
    {
        using var reader = new StreamReader(stream);

        bool hexFormat = false;
        bool currentIsTiger2 = false;
        byte[]? message = null;
        bool skipMessage = false;
        int index = 0;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.TrimEnd();
            string collapsed = trimmed.TrimStart();

            if (collapsed.Length == 0 || collapsed[0] == '#')
                continue;

            // Labels may carry arbitrary whitespace before the colon (e.g. "option  : variant = tiger2"),
            // so split on the first colon and trim the label rather than matching a fixed prefix.
            int colon = collapsed.IndexOf(':');
            if (colon < 0)
                continue;

            string label = collapsed[..colon].TrimEnd();
            string value = collapsed[(colon + 1)..];

            switch (label)
            {
                case "m-format":
                    hexFormat = value.Trim().Equals("hex", StringComparison.OrdinalIgnoreCase);
                    break;

                case "option":
                    if (VariantOption().IsMatch(value)) currentIsTiger2 = true;
                    break;

                case "m":
                    if (value.StartsWith(' ')) value = value[1..];
                    message = hexFormat ? DecodeHexMessage(value, out skipMessage) : Encoding.ASCII.GetBytes(value);
                    if (!hexFormat) skipMessage = false;
                    break;

                case "h" when message is not null:
                    if (!skipMessage && currentIsTiger2 == tiger2)
                    {
                        string hex = value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
                        yield return new MessageDigestKnownAnswer
                        {
                            Name = source is null ? $"{Label(currentIsTiger2)} #{index}" : $"{source} {Label(currentIsTiger2)} #{index}",
                            Message = message,
                            Digest = Convert.FromHexString(hex),
                        };
                        index++;
                    }

                    message = null;
                    skipMessage = false;
                    break;

                default:
                    // Any other metadata line (mode, hash, h-format, title, …) is ignored.
                    break;
            }
        }
    }

    /// <summary>
    /// Returns the short variant label used in a vector's display name.
    /// </summary>
    /// <param name="isTiger2"><see langword="true" /> for the Tiger2 variant.</param>
    /// <returns><c>"Tiger2"</c> or <c>"Tiger"</c>.</returns>
    private static string Label(bool isTiger2) => isTiger2 ? "Tiger2" : "Tiger";

    /// <summary>
    /// Decodes a hex-format message value: literal hex, a <c>repeat HH x N</c> run, or an unmodelled form (for example
    /// <c>runlength x N</c>) which sets <paramref name="skip" /> and returns an empty array.
    /// </summary>
    /// <param name="value">The text following <c>m:</c> under an <c>m-format: hex</c> directive.</param>
    /// <param name="skip">
    /// Set to <see langword="true" /> when the value cannot be decoded and the row must be skipped.
    /// </param>
    /// <returns>The decoded bytes, or an empty array when <paramref name="skip" /> is set.</returns>
    private static byte[] DecodeHexMessage(string value, out bool skip)
    {
        skip = false;
        string v = value.Trim();

        Match repeat = RepeatRun().Match(v);
        if (repeat.Success)
        {
            byte unit = Convert.ToByte(repeat.Groups[1].Value, 16);
            int count = int.Parse(repeat.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            byte[] buffer = new byte[count];
            Array.Fill(buffer, unit);
            return buffer;
        }

        // A bare hex payload (even length of hex digits) is decoded directly; anything else (e.g. "runlength x N")
        // is a form this reader does not model, so the row is skipped rather than guessed.
        if (v.Length > 0 && v.Length % 2 == 0 && HexOnly().IsMatch(v))
            return Convert.FromHexString(v);

        skip = true;
        return [];
    }

    /// <summary>
    /// Matches the <c>option: variant = tiger2</c> directive.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"variant\s*=\s*tiger2", RegexOptions.IgnoreCase)]
    private static partial Regex VariantOption();

    /// <summary>
    /// Matches a <c>repeat HH x N</c> run description.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^repeat\s+([0-9a-fA-F]{2})\s+x\s+(\d+)$")]
    private static partial Regex RepeatRun();

    /// <summary>
    /// Matches a string of hex digits only.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("^[0-9a-fA-F]+$")]
    private static partial Regex HexOnly();
}
