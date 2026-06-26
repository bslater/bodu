// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocSentenceBoundary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides conservative detection of the first sentence boundary within documentation prose, shared by the
/// content-quality analyzer and its code fix so they always agree on where (and whether) prose may be split.
/// </summary>
/// <remarks>
/// The detector deliberately refuses ambiguous cases. A boundary is a period followed by horizontal whitespace
/// where the preceding token is an ordinary word — not a known abbreviation (<c>e.g.</c>, <c>i.e.</c>,
/// <c>etc.</c>, …), not a dotted qualified name (<c>System.String</c>), not a decimal or version number, and not
/// part of an ellipsis. When no unambiguous boundary exists the detector returns <c>-1</c>, so the rule does not
/// fire and the code fix never performs a questionable split.
/// </remarks>
public static class XmlDocSentenceBoundary
{
    /// <summary>
    /// The set of known abbreviations written without an internal period whose trailing period must not be treated
    /// as a sentence boundary.
    /// </summary>
    private static readonly HashSet<string> s_abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "e.g", "i.e", "etc", "vs", "cf", "al", "viz", "ca", "approx",
        "mr", "mrs", "ms", "dr", "prof", "st", "fig", "no", "vol", "ch", "pp", "ex",
    };

    /// <summary>
    /// Finds the index of the period that ends the first sentence in the supplied prose.
    /// </summary>
    /// <param name="content">The prose to scan.</param>
    /// <returns>The zero-based index of the terminating period, or <c>-1</c> when no unambiguous boundary exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content" /> is <see langword="null" />.</exception>
    public static int FindFirstSentenceBoundary(string content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));

        for (var i = 0; i < content.Length - 1; i++)
        {
            if (content[i] != '.') continue;

            var next = content[i + 1];
            if (next != ' ' && next != '\t') continue;

            // Skip ellipses ("..." or "..").
            if (i > 0 && content[i - 1] == '.') continue;
            if (i + 2 < content.Length && content[i + 2] == '.') continue;

            // Skip decimals / version numbers (a digit immediately precedes the period).
            if (i > 0 && char.IsDigit(content[i - 1])) continue;

            var token = ReadPrecedingToken(content, i);

            // Skip dotted qualified names and abbreviations written with internal periods (System.String, e.g.).
            if (token.IndexOf('.') >= 0) continue;

            // Skip known abbreviations written without an internal period (etc, vs, Dr, …).
            if (s_abbreviations.Contains(token)) continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    /// Reads the run of non-whitespace characters ending immediately before the period at the given index.
    /// </summary>
    /// <param name="content">The prose being scanned.</param>
    /// <param name="periodIndex">The index of the candidate sentence-terminating period.</param>
    /// <returns>The token of non-whitespace characters preceding the period.</returns>
    private static string ReadPrecedingToken(string content, int periodIndex)
    {
        var start = periodIndex;
        while (start > 0 && !IsWhitespace(content[start - 1]))
        {
            start--;
        }

        // The token is the run of non-whitespace characters ending immediately before the period.
        return content.Substring(start, periodIndex - start);
    }

    /// <summary>
    /// Determines whether the given character is horizontal or vertical whitespace.
    /// </summary>
    /// <param name="ch">The character to test.</param>
    /// <returns><see langword="true" /> when the character is a space, tab, carriage return, or line feed; otherwise <see langword="false" />.</returns>
    private static bool IsWhitespace(char ch) =>
        ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
}
