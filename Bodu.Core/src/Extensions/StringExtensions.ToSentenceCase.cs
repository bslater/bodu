// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSentenceCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with the first letter of every sentence capitalised and every other
    /// letter lower-cased, using <see cref="CultureInfo.InvariantCulture" />.
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
    /// A new sentence starts at the beginning of the input and after every <c>.</c>, <c>!</c>, or <c>?</c>.
    /// All other letters are lower-cased. When <see cref="SentenceCaseOptions.PreserveAcronyms" /> is set,
    /// fully-uppercase tokens in the original input are restored after the global lower-casing pass.
    /// </remarks>
    public static string ToSentenceCase(this string value, SentenceCaseOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return value;

        bool preserveAcronyms = (options & SentenceCaseOptions.PreserveAcronyms) == SentenceCaseOptions.PreserveAcronyms;

        StringBuilder builder = new(value.Length);
        bool capitaliseNext = true;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetter(c))
            {
                builder.Append(capitaliseNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                capitaliseNext = false;
            }
            else
            {
                builder.Append(c);
                if (c is '.' or '!' or '?') capitaliseNext = true;
            }
        }

        if (preserveAcronyms)
        {
            RestoreAcronymWords(value, builder);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Walks the original <paramref name="source" /> and copies any fully-uppercase letter run back into the
    /// already-built <paramref name="builder" />, replacing the lower-cased substitute installed during the
    /// main casing pass.
    /// </summary>
    /// <param name="source">The original input string.</param>
    /// <param name="builder">The builder containing the lower-cased sentence-case output.</param>
    private static void RestoreAcronymWords(string source, StringBuilder builder)
    {
        int i = 0;
        while (i < source.Length)
        {
            if (!char.IsLetter(source[i])) { i++; continue; }

            int start = i;
            while (i < source.Length && char.IsLetter(source[i])) i++;

            int length = i - start;
            bool isAcronym = true;
            for (int j = start; j < i; j++)
            {
                if (!char.IsUpper(source[j])) { isAcronym = false; break; }
            }

            if (isAcronym && length > 1)
            {
                for (int j = 0; j < length; j++) builder[start + j] = source[start + j];
            }
        }
    }
}
