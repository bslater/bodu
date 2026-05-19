// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.WordSplitting.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Splits <paramref name="value" /> into a sequence of word tokens using CamelCase / PascalCase boundaries
    /// and common identifier separators (whitespace, <c>-</c>, <c>_</c>, <c>.</c>, <c>:</c>, <c>;</c>,
    /// <c>/</c>).
    /// </summary>
    /// <param name="value">The string to tokenise.</param>
    /// <returns>The detected words in source order, with separators discarded.</returns>
    /// <remarks>
    /// <para>
    /// Boundaries are detected as: separator characters; lowercase-to-uppercase transitions
    /// (<c>"helloWorld"</c> → <c>["hello", "World"]</c>); and uppercase-run-to-lowercase transitions to
    /// preserve acronyms (<c>"HTMLParser"</c> → <c>["HTML", "Parser"]</c>); and letter-to-digit transitions
    /// (<c>"hello42"</c> → <c>["hello", "42"]</c>).
    /// </para>
    /// <para>
    /// Used internally by every casing converter (<see cref="ToCamelCase(string)" />,
    /// <see cref="ToPascalCase(string)" />, <see cref="ToSnakeCase(string)" />,
    /// <see cref="ToKebabCase(string)" />, <see cref="ToTrainCase(string)" />,
    /// <see cref="ToConstantCase(string)" />, <see cref="ToDotCase(string)" />).
    /// </para>
    /// </remarks>
    internal static List<string> EnumerateWords(string value)
    {
        List<string> words = new();
        if (value.Length == 0) return words;

        StringBuilder current = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
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

            char prev = current[^1];
            bool isUpper = char.IsUpper(c);
            bool prevWasLower = char.IsLower(prev);
            bool prevWasDigit = char.IsDigit(prev);
            bool prevWasLetter = char.IsLetter(prev);
            bool isDigit = char.IsDigit(c);
            bool isLetter = char.IsLetter(c);

            bool boundary = false;
            if (isUpper && prevWasLower) boundary = true;
            else if (isLetter && prevWasDigit) boundary = true;
            else if (isDigit && prevWasLetter) boundary = true;
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
}
