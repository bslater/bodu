// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.WordSplitting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Splits <paramref name="value" /> into a sequence of word tokens using CamelCase / PascalCase boundaries and
    /// common identifier separators (whitespace, <c>-</c>, <c>_</c>, <c>.</c>, <c>:</c>, <c>;</c>, <c>/</c>).
    /// </summary>
    /// <param name="value">The string to tokenise.</param>
    /// <returns>The detected words in source order, with separators discarded.</returns>
    /// <remarks>
    /// <para>
    /// Boundaries are detected as: separator characters; lowercase-to-uppercase transitions (<c>"helloWorld"</c> →
    /// <c>["hello", "World"]</c>); uppercase-run-to-lowercase transitions to preserve acronyms (<c>"HTMLParser"</c> →
    /// <c>["HTML", "Parser"]</c>); and digit-to-letter transitions (<c>"42hello"</c> → <c>["42", "hello"]</c>).
    /// </para>
    /// <para>
    /// A letter-to-digit transition is intentionally <em>not</em> a boundary, so a trailing version or count stays
    /// attached to its word (<c>"user42"</c> → <c>["user42"]</c>, <c>"v1"</c> → <c>["v1"]</c>).
    /// </para>
    /// <para>
    /// Used internally by every casing converter (<see cref="ToCamelCase(string)" />,
    /// <see cref="ToPascalCase(string)" />, <see cref="ToSnakeCase(string)" />, <see cref="ToKebabCase(string)" />,
    /// <see cref="ToTrainCase(string)" />, <see cref="ToConstantCase(string)" />, <see cref="ToDotCase(string)" />).
    /// </para>
    /// </remarks>
    internal static List<string> EnumerateWords(string value)
    {
        List<string> words = new();
        if (value.Length == 0) return words;

        StringBuilder current = new();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (IsSeparator(c))
            {
                FlushWord(words, current);
                continue;
            }

            if (current.Length == 0)
            {
                current.Append(c);
                continue;
            }

            var prev = current[^1];
            var isUpper = char.IsUpper(c);
            var prevWasLower = char.IsLower(prev);
            var prevWasDigit = char.IsDigit(prev);
            var isLetter = char.IsLetter(c);

            var boundary = false;
            if (isUpper && prevWasLower)
            {
                boundary = true;
            }
            else if (isLetter && prevWasDigit)
            {
                boundary = true;
            }
            else if (isUpper && current.Length >= 2 && char.IsUpper(prev) && i + 1 < value.Length && char.IsLower(value[i + 1]))
            {
                // Acronym to word transition: last upper char joins the next word.
                FlushWord(words, current);
                current.Append(c);
                continue;
            }

            if (boundary)
            {
                FlushWord(words, current);
            }

            current.Append(c);
        }

        FlushWord(words, current);
        return words;

        static bool IsSeparator(char c) =>
            char.IsWhiteSpace(c) || c is '-' or '_' or '.' or ':' or ';' or '/' or '\\';

        static void FlushWord(List<string> sink, StringBuilder buffer)
        {
            if (buffer.Length == 0) return;
            sink.Add(buffer.ToString());
            buffer.Clear();
        }
    }

    /// <summary>
    /// Splits <paramref name="value" /> into word tokens using acronym-aware tokenisation driven by
    /// <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to tokenise.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration.</param>
    /// <returns>The detected words in source order, with separators discarded.</returns>
    /// <remarks>
    /// <para>
    /// The input is first divided into chunks on any character that is neither a letter, a digit, nor an apostrophe.
    /// Each chunk is then resolved in the following order:
    /// </para>
    /// <para>
    /// 1. A chunk that case-insensitively equals a known acronym is emitted as a single token using the catalogue's
    /// canonical spelling.
    /// </para>
    /// <para>
    /// 2. When <see cref="WordCasingOptions.PreserveMixedCaseWords" /> is set, a chunk shaped as one lowercase letter,
    /// then an uppercase letter, then anything (for example <c>iPhone</c>, <c>eBay</c>) — and which is not entirely
    /// uppercase — is emitted verbatim as a single token.
    /// </para>
    /// <para>
    /// 3. Otherwise the chunk is case-split into sub-words at lowercase-to-uppercase and digit-to-letter transitions
    /// and at acronym-to-word boundaries. Any all-uppercase sub-word that is not itself a known acronym is then offered
    /// for greedy decomposition into a run of two or more known acronyms.
    /// </para>
    /// </remarks>
    internal static List<string> EnumerateWords(string value, WordCasingOptions options)
    {
        List<string> words = new();
        if (value.Length == 0) return words;

        Dictionary<string, string> canonical = BuildAcronymLookup(options.Acronyms);

        StringBuilder chunk = new();
        for (var i = 0; i <= value.Length; i++)
        {
            var c = i < value.Length ? value[i] : '\0';
            var isChunkChar = i < value.Length && (char.IsLetterOrDigit(c) || c == '\'');
            if (isChunkChar)
            {
                chunk.Append(c);
                continue;
            }

            if (chunk.Length > 0)
            {
                ProcessChunk(chunk.ToString(), canonical, options, words);
                chunk.Clear();
            }
        }

        return words;
    }

    /// <summary>
    /// Builds a case-insensitive lookup that maps an upper-cased acronym key to its canonical spelling.
    /// </summary>
    /// <param name="acronyms">The acronym catalogue.</param>
    /// <returns>A dictionary keyed by the invariant upper-case form of each acronym.</returns>
    private static Dictionary<string, string> BuildAcronymLookup(IReadOnlyCollection<string> acronyms)
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (var acronym in acronyms)
        {
            if (string.IsNullOrEmpty(acronym)) continue;
            var key = acronym.ToUpperInvariant();
            map[key] = acronym;
        }

        return map;
    }

    /// <summary>
    /// Resolves a single separator-free chunk into one or more word tokens and appends them to <paramref name="sink" />
    /// .
    /// </summary>
    /// <param name="chunk">The separator-free chunk to resolve.</param>
    /// <param name="canonical">The canonical acronym lookup keyed by upper-case form.</param>
    /// <param name="options">The casing configuration.</param>
    /// <param name="sink">The token list receiving the resolved words.</param>
    private static void ProcessChunk(
        string chunk,
        Dictionary<string, string> canonical,
        WordCasingOptions options,
        List<string> sink)
    {
        if (chunk.Length == 0) return;

        // Whole-chunk acronym match (handles ipv6 -> IPv6, sha256 -> SHA256, oauth -> OAuth).
        if (canonical.TryGetValue(chunk.ToUpperInvariant(), out var wholeAcronym))
        {
            sink.Add(wholeAcronym);
            return;
        }

        // Whole-chunk mixed-case brand word (iPhone, eBay): one lower letter, then an upper letter.
        if (options.PreserveMixedCaseWords && IsMixedCaseBrandWord(chunk))
        {
            sink.Add(chunk);
            return;
        }

        foreach (var subWord in CaseSplit(chunk))
        {
            AppendSubWord(subWord, canonical, sink);
        }
    }

    /// <summary>
    /// Determines whether <paramref name="chunk" /> matches the mixed-case brand-word shape: exactly one leading
    /// lowercase letter followed by an uppercase letter, then any remaining characters (for example <c>iPhone</c> or
    /// <c>eBay</c>).
    /// </summary>
    /// <param name="chunk">The chunk to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the chunk is a mixed-case brand word; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Because the first character must be lower-case, such a chunk can never be entirely upper-case, so no additional
    /// all-caps exclusion check is required.
    /// </remarks>
    private static bool IsMixedCaseBrandWord(string chunk) =>
        chunk.Length >= 2 && char.IsLower(chunk[0]) && char.IsUpper(chunk[1]);

    /// <summary>
    /// Splits a separator-free chunk into sub-words at lowercase-to-uppercase, digit-to-letter, and acronym-to-word
    /// boundaries.
    /// </summary>
    /// <param name="chunk">The separator-free chunk to split.</param>
    /// <returns>The chunk's sub-words in source order.</returns>
    private static List<string> CaseSplit(string chunk)
    {
        List<string> result = new();
        StringBuilder current = new();
        for (var i = 0; i < chunk.Length; i++)
        {
            var c = chunk[i];
            if (current.Length == 0)
            {
                current.Append(c);
                continue;
            }

            var prev = current[^1];
            var isUpper = char.IsUpper(c);
            var prevWasLower = char.IsLower(prev);
            var prevWasDigit = char.IsDigit(prev);
            var isLetter = char.IsLetter(c);

            if (isUpper && prevWasLower)
            {
                Flush(result, current);
            }
            else if (isLetter && prevWasDigit)
            {
                Flush(result, current);
            }
            else if (isUpper && current.Length >= 1 && char.IsUpper(prev) && i + 1 < chunk.Length && char.IsLower(chunk[i + 1]))
            {
                // Acronym-to-word boundary: the trailing upper char starts the next word.
                Flush(result, current);
            }

            current.Append(c);
        }

        Flush(result, current);
        return result;

        static void Flush(List<string> sink, StringBuilder buffer)
        {
            if (buffer.Length == 0) return;
            sink.Add(buffer.ToString());
            buffer.Clear();
        }
    }

    /// <summary>
    /// Appends a single case-split sub-word to <paramref name="sink" />, decomposing an all-uppercase sub-word into a
    /// run of known acronyms when a full greedy decomposition exists.
    /// </summary>
    /// <param name="subWord">The sub-word produced by case-splitting.</param>
    /// <param name="canonical">The canonical acronym lookup keyed by upper-case form.</param>
    /// <param name="sink">The token list receiving the resolved words.</param>
    private static void AppendSubWord(string subWord, Dictionary<string, string> canonical, List<string> sink)
    {
        if (subWord.Length == 0) return;

        // An all-uppercase run longer than one character that is not itself a known acronym may be a run of
        // concatenated acronyms (for example JSONRPCAPI -> JSON, RPC, API).
        if (subWord.Length > 1 && IsAllUpperLettersOrDigits(subWord)
            && !canonical.ContainsKey(subWord.ToUpperInvariant()))
        {
            List<string>? decomposed = DecomposeAcronymRun(subWord, canonical);
            if (decomposed is not null)
            {
                sink.AddRange(decomposed);
                return;
            }
        }

        sink.Add(subWord);
    }

    /// <summary>
    /// Attempts a greedy longest-prefix decomposition of an all-uppercase sub-word into a sequence of two or more known
    /// acronyms.
    /// </summary>
    /// <param name="subWord">The all-uppercase sub-word to decompose.</param>
    /// <param name="canonical">The canonical acronym lookup keyed by upper-case form.</param>
    /// <returns>
    /// The canonical acronym spellings when the entire sub-word decomposes into at least two known acronyms; otherwise
    /// <see langword="null" />.
    /// </returns>
    private static List<string>? DecomposeAcronymRun(string subWord, Dictionary<string, string> canonical)
    {
        var upper = subWord.ToUpperInvariant();
        List<string> parts = new();
        var index = 0;
        while (index < upper.Length)
        {
            string? best = null;
            var bestLength = 0;
            for (var length = upper.Length - index; length >= 1; length--)
            {
                var candidate = upper.Substring(index, length);
                if (canonical.TryGetValue(candidate, out var spelling))
                {
                    best = spelling;
                    bestLength = length;
                    break;
                }
            }

            if (best is null) return null;

            parts.Add(best);
            index += bestLength;
        }

        return parts.Count >= 2 ? parts : null;
    }

    /// <summary>
    /// Determines whether every character in <paramref name="value" /> is an upper-case letter or a digit.
    /// </summary>
    /// <param name="value">The string to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the string contains only upper-case letters and digits; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool IsAllUpperLettersOrDigits(string value)
    {
        if (value.Length == 0) return false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsLetter(c))
            {
                if (!char.IsUpper(c)) return false;
            }
            else if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
