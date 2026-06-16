// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToTitleCase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// The control character (U+001F unit separator) prefixing a synthetic punctuation token in the phrase token
    /// stream. It cannot collide with any letter, digit, or punctuation produced by tokenisation.
    /// </summary>
    private const char PunctuationMarker = '\u001F';

    /// <summary>
    /// The connective words lower-cased when <see cref="TitleCaseOptions.LowerCaseSmallWords" /> is set on the
    /// flag-based overload.
    /// </summary>
    private static readonly string[] s_titleCaseSmallWords =
    [
        "a", "an", "and", "as", "at", "but", "by", "for", "in", "nor",
        "of", "on", "or", "per", "so", "the", "to", "up", "via", "vs", "yet",
    ];

    /// <summary>
    /// Returns <paramref name="value" /> with the first character of every word capitalised and the rest lower-cased,
    /// using <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The title-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string ToTitleCase(this string value) =>
        ToTitleCase(value, TitleCaseOptions.None);

    /// <summary>
    /// Returns <paramref name="value" /> in title case under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">Flags that adjust acronym preservation and small-word handling.</param>
    /// <returns>The title-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The flag-based overload maps onto <see cref="WordCasingOptions" /> with an empty acronym catalogue, so only
    /// fully-uppercase input tokens are preserved as acronyms — lower-case words are never promoted to a canonical
    /// acronym spelling.
    /// </remarks>
    public static string ToTitleCase(this string value, TitleCaseOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        bool preserveAcronyms = (options & TitleCaseOptions.PreserveAcronyms) == TitleCaseOptions.PreserveAcronyms;
        bool lowerSmall = (options & TitleCaseOptions.LowerCaseSmallWords) == TitleCaseOptions.LowerCaseSmallWords;

        WordCasingOptions casing = new()
        {
            Acronyms = Array.Empty<string>(),
            MinorWords = s_titleCaseSmallWords,
            PreserveAcronyms = preserveAcronyms,
            LowerCaseMinorWords = lowerSmall,
        };

        return ToTitleCase(value, casing);
    }

    /// <summary>
    /// Returns <paramref name="value" /> in title case under the supplied <paramref name="options" />, applying
    /// acronym-aware tokenisation, mixed-case-word preservation, and culture-sensitive casing.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, minor-word, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The title-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When the input contains white-space it is treated as a phrase: words are split on white-space, a joining hyphen
    /// is kept attached, and trailing sentence punctuation is preserved. When the input contains no white-space it is
    /// treated as a compound identifier and every separator becomes a single space.
    /// </para>
    /// <para>
    /// Known acronyms and fully-uppercase tokens keep their acronym spelling when
    /// <see cref="WordCasingOptions.PreserveAcronyms" /> is set; recognised mixed-case words are emitted verbatim when
    /// <see cref="WordCasingOptions.PreserveMixedCaseWords" /> is set. When
    /// <see cref="WordCasingOptions.LowerCaseMinorWords" /> is set, minor words that are neither the first nor the last
    /// word are down-cased. The first letter after an apostrophe is capitalised so that personal names such as
    /// <c>o'connor</c> become <c>O'Connor</c>.
    /// </para>
    /// </remarks>
    public static string ToTitleCase(this string value, WordCasingOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        if (value.Length == 0) return string.Empty;

        bool phraseMode = ContainsWhiteSpace(value);
        List<string> tokens = phraseMode
            ? EnumeratePhraseWords(value, options)
            : EnumerateWords(value, options);
        return tokens.Count == 0 ? string.Empty : RenderTitleWords(tokens, options);
    }

    /// <summary>
    /// Renders a tokenised word list as a single space-joined title-case string.
    /// </summary>
    /// <param name="tokens">The word tokens, including any synthetic punctuation markers.</param>
    /// <param name="options">The acronym, minor-word, and culture configuration.</param>
    /// <returns>The rendered title-case string.</returns>
    /// <remarks>
    /// A token whose first character is <see cref="PunctuationMarker" /> carries a literal punctuation run produced by
    /// <see cref="EnumeratePhraseWords(string, WordCasingOptions)" /> and is emitted verbatim instead of being
    /// capitalised.
    /// </remarks>
    private static string RenderTitleWords(List<string> tokens, WordCasingOptions options)
    {
        HashSet<string> minorWords = BuildMinorWordSet(options.MinorWords);
        CultureInfo culture = options.Culture;

        int realWordCount = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            if (!IsPunctuationMarker(tokens[i])) realWordCount++;
        }

        StringBuilder builder = new();
        bool needsSpace = false;
        int realRank = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i];
            if (IsPunctuationMarker(token))
            {
                // A joining hyphen attaches directly; a terminator adds a trailing space.
                string punctuation = token[1..];
                builder.Append(punctuation);
                needsSpace = punctuation is not "-";
                continue;
            }

            if (needsSpace && builder.Length > 0) builder.Append(' ');
            needsSpace = true;

            bool isInterior = realRank > 0 && realRank < realWordCount - 1;
            realRank++;

            if (options.PreserveAcronyms && IsAllUpper(token))
            {
                builder.Append(token);
                continue;
            }

            if (options.PreserveMixedCaseWords && IsPreservedMixedCaseWord(token))
            {
                builder.Append(token);
                continue;
            }

            if (options.LowerCaseMinorWords && isInterior && minorWords.Contains(token))
            {
                builder.Append(token.ToLower(culture));
                continue;
            }

            builder.Append(CapitalizeWord(token, culture));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits a white-space-containing phrase into word tokens interleaved with synthetic punctuation markers that
    /// capture joining hyphens and sentence terminators.
    /// </summary>
    /// <param name="value">The phrase to tokenise.</param>
    /// <param name="options">The acronym and mixed-case configuration.</param>
    /// <returns>
    /// The word tokens in source order. A token prefixed with <see cref="PunctuationMarker" /> carries literal
    /// punctuation to be emitted verbatim by <see cref="RenderTitleWords" />.
    /// </returns>
    private static List<string> EnumeratePhraseWords(string value, WordCasingOptions options)
    {
        Dictionary<string, string> canonical = BuildAcronymLookup(options.Acronyms);
        List<string> tokens = new();

        int i = 0;
        while (i < value.Length)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c) || c == '\'')
            {
                int start = i;
                while (i < value.Length && (char.IsLetterOrDigit(value[i]) || value[i] == '\'')) i++;
                string word = value[start..i];
                ProcessChunk(word, canonical, options, tokens);
                continue;
            }

            int sepStart = i;
            while (i < value.Length && !(char.IsLetterOrDigit(value[i]) || value[i] == '\'')) i++;
            string separator = value[sepStart..i];
            EmitSeparatorMarker(separator, sepStart == 0, i == value.Length, tokens);
        }

        return tokens;
    }

    /// <summary>
    /// Appends a synthetic punctuation marker for a phrase separator run when the run carries punctuation that the
    /// title-case contract preserves.
    /// </summary>
    /// <param name="separator">The raw separator run.</param>
    /// <param name="isLeading">Whether the run sits at the start of the input.</param>
    /// <param name="isTrailing">Whether the run sits at the end of the input.</param>
    /// <param name="tokens">The token list receiving the marker.</param>
    private static void EmitSeparatorMarker(string separator, bool isLeading, bool isTrailing, List<string> tokens)
    {
        // A leading run never contributes punctuation.
        if (isLeading) return;

        char joiner = '\0';
        char terminator = '\0';
        foreach (char c in separator)
        {
            if (c == '-') joiner = '-';
            else if (c is '.' or ',' or '!' or '?' or ';' or ':') terminator = c;
        }

        // A terminator wins over a joiner when both appear; trailing joiners are dropped.
        if (terminator != '\0')
        {
            tokens.Add(string.Concat(PunctuationMarker.ToString(), terminator.ToString()));
        }
        else if (joiner == '-' && !isTrailing)
        {
            tokens.Add(string.Concat(PunctuationMarker.ToString(), joiner.ToString()));
        }
    }

    /// <summary>
    /// Determines whether <paramref name="token" /> is a synthetic punctuation marker rather than a word.
    /// </summary>
    /// <param name="token">The token to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the token carries literal punctuation; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsPunctuationMarker(string token) =>
        token.Length > 0 && token[0] == PunctuationMarker;

    /// <summary>
    /// Determines whether <paramref name="value" /> contains at least one white-space character.
    /// </summary>
    /// <param name="value">The string to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the string contains white-space; otherwise <see langword="false" />.
    /// </returns>
    private static bool ContainsWhiteSpace(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i])) return true;
        }

        return false;
    }

    /// <summary>
    /// Builds a case-insensitive set of minor words for fast interior-word lookup.
    /// </summary>
    /// <param name="minorWords">The configured minor-word list.</param>
    /// <returns>An ordinal, case-insensitive set of the minor words.</returns>
    private static HashSet<string> BuildMinorWordSet(IReadOnlyCollection<string> minorWords)
    {
        HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);
        foreach (string word in minorWords)
        {
            if (!string.IsNullOrEmpty(word)) set.Add(word);
        }

        return set;
    }

    /// <summary>
    /// Returns <see langword="true" /> when every letter in <paramref name="word" /> is upper case and the word
    /// contains at least one letter.
    /// </summary>
    /// <param name="word">The word to inspect.</param>
    /// <returns><see langword="true" /> for all-uppercase words; otherwise <see langword="false" />.</returns>
    private static bool IsAllUpper(string word)
    {
        if (word.Length == 0) return false;

        bool hasLetter = false;
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            if (char.IsLetter(c))
            {
                hasLetter = true;
                if (!char.IsUpper(c)) return false;
            }
        }

        return hasLetter;
    }
}
