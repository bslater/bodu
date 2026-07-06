// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc8439VectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the RFC 8439 (<c>ChaCha20 &amp; Poly1305</c>) appendix test vectors. Each appendix section (for example
/// <c>A.2. ChaCha20 Encryption</c>) contains numbered <c>Test Vector #N</c> blocks whose labelled fields (<c>Key:</c>,
/// <c>Nonce:</c>, <c>Plaintext:</c>, <c>Ciphertext:</c>, …) are rendered as offset-prefixed hex dumps, interleaved with
/// scalar lines such as <c>Initial Block Counter = N</c>. The reader extracts the hex bytes for each field, ignores the
/// trailing ASCII gutter and the page headers/footers, and yields one <see cref="Rfc8439TestVector" /> per block within
/// the requested section.
/// </summary>
public static partial class Rfc8439VectorReader
{
    /// <summary>
    /// Reads the numbered test vectors within the appendix section whose heading starts with
    /// <paramref name="sectionTitle" />.
    /// </summary>
    /// <param name="stream">A readable stream over the RFC 8439 source text.</param>
    /// <param name="sectionTitle">The section heading prefix to select, for example <c>A.2.  ChaCha20 Encryption</c>.</param>
    /// <returns>The section's test vectors, in source order.</returns>
    public static IEnumerable<Rfc8439TestVector> Read(Stream stream, string sectionTitle)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        bool inSection = false;
        int? number = null;
        string? field = null;
        uint? blockCounter = null;
        var fields = new Dictionary<string, List<byte>>(StringComparer.OrdinalIgnoreCase);

        Rfc8439TestVector Snapshot() => new(
            number!.Value,
            fields.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase),
            blockCounter);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();

            if (!inSection)
            {
                // Match the real heading only — the table-of-contents entry carries a trailing dot leader and page
                // number, so an exact (whitespace-normalized) match rejects it.
                if (Normalize(trimmed).Equals(Normalize(sectionTitle), StringComparison.OrdinalIgnoreCase))
                    inSection = true;
                continue;
            }

            // A subsequent "X.Y  Title" appendix heading ends the current section.
            if (SectionHeadingPattern().IsMatch(trimmed))
            {
                if (number is not null)
                    yield return Snapshot();

                yield break;
            }

            Match vector = TestVectorPattern().Match(trimmed);
            if (vector.Success)
            {
                if (number is not null)
                    yield return Snapshot();

                number = int.Parse(vector.Groups[1].Value, CultureInfo.InvariantCulture);
                field = null;
                blockCounter = null;
                fields = new Dictionary<string, List<byte>>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (number is null)
                continue;

            Match counter = BlockCounterPattern().Match(trimmed);
            if (counter.Success)
            {
                blockCounter = uint.Parse(counter.Groups[1].Value, CultureInfo.InvariantCulture);
                field = null;
                continue;
            }

            Match label = LabelPattern().Match(trimmed);
            if (label.Success)
            {
                field = label.Groups[1].Value.Trim();
                fields[field] = [];
                continue;
            }

            if (field is null)
                continue;

            Match dump = HexDumpPattern().Match(line);
            if (dump.Success)
            {
                foreach (string token in dump.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    fields[field].Add(byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
        }

        if (number is not null)
            yield return Snapshot();
    }

    /// <summary>Collapses runs of whitespace to single spaces so heading comparisons ignore column alignment.</summary>
    /// <param name="value">The text to normalize.</param>
    /// <returns>The trimmed, single-spaced form.</returns>
    private static string Normalize(string value) =>
        WhitespaceRunPattern().Replace(value.Trim(), " ");

    [GeneratedRegex(@"Test Vector #(\d+)")]
    private static partial Regex TestVectorPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunPattern();

    [GeneratedRegex(@"Initial Block Counter\s*=\s*(\d+)")]
    private static partial Regex BlockCounterPattern();

    [GeneratedRegex(@"^([A-Za-z][A-Za-z0-9 /-]*):$")]
    private static partial Regex LabelPattern();

    [GeneratedRegex(@"^\s*\d+\s+([0-9a-fA-F]{2}(?:\s[0-9a-fA-F]{2})*)\s{2,}")]
    private static partial Regex HexDumpPattern();

    [GeneratedRegex(@"^[A-Z]\.\d+\.\s")]
    private static partial Regex SectionHeadingPattern();
}
