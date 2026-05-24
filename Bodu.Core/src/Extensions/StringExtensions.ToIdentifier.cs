// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToIdentifier.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> into a syntactically valid C# identifier by removing characters that are not
    /// permitted in identifiers and preserving the original casing of the remaining characters.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new identifier-safe string. If the cleaned form does not begin with a permitted identifier-start character, an
    /// underscore is prepended.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to <c>value.ToIdentifier(IdentifierCase.Preserve)</c>. When the input contains no valid identifier
    /// characters at all, a single underscore is returned so the result is still a valid identifier.
    /// </remarks>
    public static string ToIdentifier(this string value) =>
        value.ToIdentifier(IdentifierCase.Preserve);

    /// <summary>
    /// Converts <paramref name="value" /> into a syntactically valid C# identifier and reshapes the detected words to
    /// <paramref name="identifierCase" />.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <param name="identifierCase">The target casing applied to the detected words.</param>
    /// <returns>
    /// A new identifier-safe string in the chosen casing. If the result would otherwise start with a digit, an
    /// underscore is prepended so the returned value still satisfies the identifier grammar.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="identifierCase" /> is not a defined <see cref="IdentifierCase" /> value.
    /// </exception>
    /// <remarks>
    /// Words are detected via the same boundary rules used by the casing converters (
    /// <see cref="EnumerateWords(string)" />). Non-letter / non-digit / non-underscore characters are treated as
    /// separators and discarded.
    /// </remarks>
    public static string ToIdentifier(this string value, IdentifierCase identifierCase)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfEnumValueIsUndefined(identifierCase);

        if (identifierCase == IdentifierCase.Preserve) return PreserveAsIdentifier(value);

        List<string> words = EnumerateWords(value);
        FilterIdentifierWords(words);

        if (words.Count == 0) return "_";

        StringBuilder builder = new(value.Length + 1);
        switch (identifierCase)
        {
            case IdentifierCase.Camel:
                builder.Append(words[0].ToLowerInvariant());
                for (var i = 1; i < words.Count; i++) AppendCapitalised(builder, words[i]);
                break;
            case IdentifierCase.Pascal:
                for (var i = 0; i < words.Count; i++) AppendCapitalised(builder, words[i]);
                break;
            case IdentifierCase.Snake:
                builder.Append(words[0].ToLowerInvariant());
                for (var i = 1; i < words.Count; i++)
                {
                    builder.Append('_');
                    builder.Append(words[i].ToLowerInvariant());
                }

                break;
        }

        if (builder.Length == 0 || !IsIdentifierStart(builder[0])) builder.Insert(0, '_');
        return builder.ToString();
    }

    /// <summary>
    /// Returns <paramref name="value" /> with every character that is not a letter, decimal digit, or underscore
    /// removed, and an underscore prepended when the surviving leading character is a digit so the result satisfies the
    /// identifier-start rule.
    /// </summary>
    /// <param name="value">The source string to scrub.</param>
    /// <returns>An identifier-safe string that preserves the surviving characters in their original casing.</returns>
    private static string PreserveAsIdentifier(string value)
    {
        StringBuilder builder = new(value.Length + 1);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c == '_') builder.Append(c);
        }

        if (builder.Length == 0) return "_";
        if (!IsIdentifierStart(builder[0])) builder.Insert(0, '_');
        return builder.ToString();
    }

    /// <summary>
    /// Strips identifier-invalid characters from each detected word and removes any word that contains no surviving
    /// characters.
    /// </summary>
    /// <param name="words">The mutable list of detected words to scrub in place.</param>
    private static void FilterIdentifierWords(List<string> words)
    {
        for (var i = words.Count - 1; i >= 0; i--)
        {
            var filtered = FilterIdentifierWord(words[i]);
            if (filtered.Length == 0) words.RemoveAt(i);
            else words[i] = filtered;
        }
    }

    /// <summary>
    /// Returns <paramref name="word" /> with every character that is not a letter, decimal digit, or underscore
    /// removed.
    /// </summary>
    /// <param name="word">The detected word to scrub.</param>
    /// <returns>The word with invalid identifier characters removed.</returns>
    private static string FilterIdentifierWord(string word)
    {
        StringBuilder builder = new(word.Length);
        foreach (var c in word)
        {
            if (char.IsLetterOrDigit(c) || c == '_') builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends <paramref name="word" /> to <paramref name="builder" /> with its first character upper-cased and the
    /// remainder lower-cased.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="word">The word to append.</param>
    private static void AppendCapitalised(StringBuilder builder, string word)
    {
        if (word.Length == 0) return;
        builder.Append(char.ToUpperInvariant(word[0]));
        if (word.Length > 1) builder.Append(word.AsSpan(1).ToString().ToLowerInvariant());
    }
}
