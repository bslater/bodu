// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NistLwcKatReader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses NIST Lightweight Cryptography (LWC) AEAD known-answer test files in the <c>.rsp</c>-style format used by
/// the official ASCON and NIST submission packages. Each vector occupies six labelled lines —
/// <c>Count</c>, <c>Key</c>, <c>Nonce</c>, <c>PT</c>, <c>AD</c>, <c>CT</c> — and is separated from the next by one or
/// more blank lines.
/// </summary>
public static class NistLwcKatReader
{
    /// <summary>
    /// Reads NIST LWC AEAD known-answer test vectors from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">
    /// A readable text stream containing the KAT data. Lines may be in any line-ending convention. Lines beginning
    /// with <c>#</c> are treated as comments and ignored, as are blank lines between records.
    /// </param>
    /// <param name="tagLength">
    /// The number of trailing bytes of the <c>CT</c> field that form the authentication tag. The KAT files in this
    /// project all carry a 16-byte tag, but the reader is generic so future-format variants can override.
    /// </param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector.</param>
    /// <returns>The parsed vectors, one per <c>Count</c> record, yielded in source order.</returns>
    /// <exception cref="FormatException">A record is missing one of the six required fields, or a value is malformed.</exception>
    public static IEnumerable<AeadKnownAnswerVector> Read(Stream stream, int tagLength = 16, string? source = null)
    {
        using var reader = new StreamReader(stream);

        int? count = null;
        string? key = null;
        string? nonce = null;
        string? pt = null;
        string? ad = null;
        string? ct = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                if (count is not null)
                {
                    yield return Build(count.Value, key, nonce, ad, pt, ct, tagLength, source);
                    count = null;
                    key = null;
                    nonce = null;
                    pt = null;
                    ad = null;
                    ct = null;
                }

                continue;
            }

            if (trimmed[0] == '#') continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0)
                throw new FormatException($"Unrecognised KAT line (no '=' separator): '{trimmed}'.");

            var label = trimmed[..eqIndex].TrimEnd();
            var value = trimmed[(eqIndex + 1)..].TrimStart();

            switch (label)
            {
                case "Count":
                    count = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "Key":
                    key = value;
                    break;
                case "Nonce":
                    nonce = value;
                    break;
                case "PT":
                    pt = value;
                    break;
                case "AD":
                    ad = value;
                    break;
                case "CT":
                    ct = value;
                    break;
                default:
                    throw new FormatException($"Unrecognised KAT field label: '{label}'.");
            }
        }

        if (count is not null)
            yield return Build(count.Value, key, nonce, ad, pt, ct, tagLength, source);
    }

    /// <summary>
    /// Assembles an <see cref="AeadKnownAnswerVector" /> from the captured field values of a single record, throwing
    /// when any required field is missing.
    /// </summary>
    private static AeadKnownAnswerVector Build(int count, string? key, string? nonce, string? ad, string? pt, string? ct, int tagLength, string? source)
    {
        if (key is null) throw new FormatException($"KAT record Count={count} is missing the Key field.");
        if (nonce is null) throw new FormatException($"KAT record Count={count} is missing the Nonce field.");
        if (pt is null) throw new FormatException($"KAT record Count={count} is missing the PT field.");
        if (ad is null) throw new FormatException($"KAT record Count={count} is missing the AD field.");
        if (ct is null) throw new FormatException($"KAT record Count={count} is missing the CT field.");

        return AeadKnownAnswerVector.FromHex(count, key, nonce, ad, pt, ct, tagLength, source);
    }
}
