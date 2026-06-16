// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HexFieldKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses line-oriented known-answer test files made up of <c>Field = value</c> lines grouped into records separated by
/// one or more blank lines. Lines beginning with <c>#</c> are comments. The reader is format-agnostic; callers map each
/// emitted field dictionary onto their strongly typed KAT record.
/// </summary>
public static class HexFieldKatReader
{
    /// <summary>
    /// Reads the records of a <c>Field = value</c> KAT file from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">
    /// A readable text stream containing the KAT data. Any line-ending convention is accepted.
    /// </param>
    /// <returns>
    /// One ordinal-keyed dictionary per record, each mapping field labels to their raw (untrimmed-interior) string
    /// values, yielded in source order.
    /// </returns>
    /// <exception cref="FormatException">
    /// A non-blank, non-comment line has no <c>=</c> separator, or a record repeats a field label.
    /// </exception>
    public static IEnumerable<Dictionary<string, string>> Read(Stream stream)
    {
        using var reader = new StreamReader(stream);

        Dictionary<string, string>? record = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                if (record is not null)
                {
                    yield return record;
                    record = null;
                }

                continue;
            }

            if (trimmed[0] == '#') continue;

            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0)
                throw new FormatException($"Unrecognised KAT line (no '=' separator): '{trimmed}'.");

            string label = trimmed[..eqIndex].TrimEnd();
            string value = trimmed[(eqIndex + 1)..].TrimStart();

            record ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!record.TryAdd(label, value))
                throw new FormatException($"KAT record repeats the field label '{label}'.");
        }

        if (record is not null)
            yield return record;
    }

    /// <summary>
    /// Returns the value of a required field from a record dictionary, throwing when the field is absent.
    /// </summary>
    /// <param name="record">The record to read from.</param>
    /// <param name="field">The field label to look up.</param>
    /// <returns>The raw string value of the field.</returns>
    /// <exception cref="FormatException">The record does not contain <paramref name="field" />.</exception>
    public static string GetRequired(IReadOnlyDictionary<string, string> record, string field) =>
        record.TryGetValue(field, out string? value)
            ? value
            : throw new FormatException($"KAT record is missing the required field '{field}'.");
}
