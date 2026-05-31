// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSentenceCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with the first letter of every sentence capitalised and every other letter
    /// lower-cased, using <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The sentence-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string ToSentenceCase(this string value) =>
        ToSentenceCase(value, SentenceCaseOptions.None);

    /// <summary>
    /// Returns <paramref name="value" /> in sentence case under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">Flags that adjust acronym preservation.</param>
    /// <returns>The sentence-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A new sentence starts at the beginning of the input and after every <c>.</c>, <c>!</c>, or <c>?</c>. The
    /// flag-based overload maps onto <see cref="WordCasingOptions" /> with an empty acronym catalogue, so only
    /// fully-uppercase input tokens are preserved as acronyms.
    /// </remarks>
    public static string ToSentenceCase(this string value, SentenceCaseOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        var preserveAcronyms = (options & SentenceCaseOptions.PreserveAcronyms) == SentenceCaseOptions.PreserveAcronyms;

        WordCasingOptions casing = new()
        {
            Acronyms = Array.Empty<string>(),
            PreserveAcronyms = preserveAcronyms,
        };

        return ToSentenceCase(value, casing);
    }

    /// <summary>
    /// Returns <paramref name="value" /> in sentence case under the supplied <paramref name="options" />, applying
    /// acronym-aware tokenisation, mixed-case-word preservation, and culture-sensitive casing.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The sentence-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When the input contains white-space its original spacing and punctuation are preserved: every letter is
    /// lower-cased except the first letter of each sentence, which is capitalised. A new sentence starts at the
    /// beginning and after every <c>.</c>, <c>!</c>, or <c>?</c>. When the input contains no white-space it is treated
    /// as a compound identifier, tokenised, and re-joined with single spaces.
    /// </para>
    /// <para>
    /// Known acronyms and fully-uppercase tokens keep their acronym spelling when
    /// <see cref="WordCasingOptions.PreserveAcronyms" /> is set; recognised mixed-case words are emitted verbatim when
    /// <see cref="WordCasingOptions.PreserveMixedCaseWords" /> is set. Only sentence terminators — never an apostrophe
    /// — trigger capitalisation, so <c>o'connor</c> becomes <c>O'connor</c>.
    /// </para>
    /// </remarks>
    public static string ToSentenceCase(this string value, WordCasingOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        return value.Length == 0
            ? string.Empty
            : ContainsWhiteSpace(value)
            ? SentenceCasePhrase(value, options)
            : SentenceCaseIdentifier(value, options);
    }

    /// <summary>
    /// Renders a white-space-containing phrase in sentence case, preserving the original spacing and punctuation.
    /// </summary>
    /// <param name="value">The phrase to render.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration.</param>
    /// <returns>The sentence-case rendering of <paramref name="value" />.</returns>
    private static string SentenceCasePhrase(string value, WordCasingOptions options)
    {
        Dictionary<string, string> canonical = BuildAcronymLookup(options.Acronyms);
        CultureInfo culture = options.Culture;

        StringBuilder builder = new(value.Length);
        var sentenceStart = true;
        var sawWord = false;
        var i = 0;
        while (i < value.Length)
        {
            var c = value[i];
            if (char.IsLetterOrDigit(c) || c == '\'')
            {
                var start = i;
                while (i < value.Length && (char.IsLetterOrDigit(value[i]) || value[i] == '\'')) i++;
                var word = value[start..i];
                builder.Append(CaseWordForSentence(word, canonical, options, culture, sentenceStart));
                sentenceStart = false;
                sawWord = true;
                continue;
            }

            builder.Append(c);
            if (c is '.' or '!' or '?') sentenceStart = true;
            i++;
        }

        // A run that contains only white-space and punctuation collapses to an empty string.
        return sawWord ? builder.ToString() : string.Empty;
    }

    /// <summary>
    /// Renders a white-space-free compound identifier in sentence case by tokenising it and re-joining the words with
    /// single spaces.
    /// </summary>
    /// <param name="value">The identifier to render.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration.</param>
    /// <returns>The sentence-case rendering of <paramref name="value" />.</returns>
    private static string SentenceCaseIdentifier(string value, WordCasingOptions options)
    {
        List<string> words = EnumerateWords(value, options);
        if (words.Count == 0) return string.Empty;

        Dictionary<string, string> canonical = BuildAcronymLookup(options.Acronyms);
        CultureInfo culture = options.Culture;

        StringBuilder builder = new(value.Length);
        for (var i = 0; i < words.Count; i++)
        {
            if (i > 0) builder.Append(' ');
            builder.Append(CaseWordForSentence(words[i], canonical, options, culture, i == 0));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Applies sentence-case rules to a single word: preserved acronyms and mixed-case words are emitted verbatim,
    /// otherwise the word is lower-cased and — when it starts a sentence — its first letter is capitalised.
    /// </summary>
    /// <param name="word">The word to case.</param>
    /// <param name="canonical">The canonical acronym lookup keyed by upper-case form.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration.</param>
    /// <param name="culture">The culture whose casing rules are applied.</param>
    /// <param name="sentenceStart">Whether the word begins a new sentence.</param>
    /// <returns>The sentence-cased word.</returns>
    private static string CaseWordForSentence(
        string word,
        Dictionary<string, string> canonical,
        WordCasingOptions options,
        CultureInfo culture,
        bool sentenceStart)
    {
        if (word.Length == 0) return word;

        if (options.PreserveAcronyms)
        {
            // A known acronym is emitted in its canonical spelling regardless of the input casing.
            if (canonical.TryGetValue(word.ToUpperInvariant(), out var canonicalSpelling))
            {
                return canonicalSpelling;
            }

            // A fully-uppercase token already in the input is preserved verbatim.
            if (IsAllUpper(word))
            {
                return word;
            }
        }

        if (options.PreserveMixedCaseWords && IsPreservedMixedCaseWord(word))
        {
            return word;
        }

        var lower = word.ToLower(culture);
        if (!sentenceStart) return lower;

        StringBuilder builder = new(lower.Length);
        var capitalised = false;
        for (var i = 0; i < lower.Length; i++)
        {
            var c = lower[i];
            if (!capitalised && char.IsLetter(c))
            {
                builder.Append(ToUpper(c, culture));
                capitalised = true;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
