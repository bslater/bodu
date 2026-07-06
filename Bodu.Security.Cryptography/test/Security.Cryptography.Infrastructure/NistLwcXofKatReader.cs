// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NistLwcXofKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses NIST Lightweight Cryptography (LWC) hash / XOF / CXOF known-answer test files in the <c>.rsp</c>-style format
/// used by the official ASCON reference package. Each vector occupies a <c>Count</c> line, a <c>Msg</c> line, an
/// optional <c>Z</c> (customization) line, and an <c>MD</c> (output) line, and is separated from the next by one or more
/// blank lines.
/// </summary>
public static class NistLwcXofKatReader
{
    /// <summary>
    /// Reads NIST LWC hash / XOF / CXOF known-answer test vectors from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">
    /// A readable text stream containing the KAT data. Lines may be in any line-ending convention. Lines beginning with
    /// <c>#</c> are treated as comments and ignored, as are blank lines between records.
    /// </param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector.</param>
    /// <returns>The parsed vectors, one per <c>Count</c> record, yielded in source order.</returns>
    /// <exception cref="FormatException">A record is missing the <c>Msg</c> or <c>MD</c> field, or a line is malformed.</exception>
    public static IEnumerable<XofKnownAnswer> Read(Stream stream, string? source = null)
    {
        using var reader = new StreamReader(stream);

        int? count = null;
        string? msg = null;
        string? z = null;
        string? md = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                if (count is not null)
                {
                    yield return Build(count.Value, msg, z, md, source);
                    count = null;
                    msg = null;
                    z = null;
                    md = null;
                }

                continue;
            }

            if (trimmed[0] == '#') continue;

            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0)
                throw new FormatException($"Unrecognised KAT line (no '=' separator): '{trimmed}'.");

            string label = trimmed[..eqIndex].TrimEnd();
            string value = trimmed[(eqIndex + 1)..].TrimStart();

            switch (label)
            {
                case "Count":
                    count = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "Msg":
                    msg = value;
                    break;
                case "Z":
                    z = value;
                    break;
                case "MD":
                    md = value;
                    break;
                default:
                    throw new FormatException($"Unrecognised KAT field label: '{label}'.");
            }
        }

        if (count is not null)
            yield return Build(count.Value, msg, z, md, source);
    }

    /// <summary>
    /// Assembles an <see cref="XofKnownAnswer" /> from the captured field values of a single record, throwing when the
    /// required <c>Msg</c> or <c>MD</c> field is absent. A missing <c>Z</c> line yields a <see langword="null" />
    /// customization (the plain-XOF case).
    /// </summary>
    /// <param name="count">The record's <c>Count</c> ordinal.</param>
    /// <param name="msg">The <c>Msg</c> field value, or <see langword="null" /> if it was absent.</param>
    /// <param name="z">The <c>Z</c> field value, or <see langword="null" /> if the record had no customization line.</param>
    /// <param name="md">The <c>MD</c> field value, or <see langword="null" /> if it was absent.</param>
    /// <param name="source">The optional citation to attach to the emitted vector.</param>
    /// <returns>The assembled vector.</returns>
    /// <exception cref="FormatException"><paramref name="msg" /> or <paramref name="md" /> is <see langword="null" />.</exception>
    private static XofKnownAnswer Build(int count, string? msg, string? z, string? md, string? source)
    {
        if (msg is null) throw new FormatException($"KAT record Count={count} is missing the Msg field.");
        if (md is null) throw new FormatException($"KAT record Count={count} is missing the MD field.");

        string label = z is null
            ? $"Count {count} (Msg {msg.Length / 2} bytes)"
            : $"Count {count} (Msg {msg.Length / 2}, Z {z.Length / 2} bytes)";

        return new XofKnownAnswer
        {
            Name = label,
            Message = Hex(msg),
            Customization = z is null ? null : Hex(z),
            Digest = Hex(md),
            Source = source,
        };
    }
}
