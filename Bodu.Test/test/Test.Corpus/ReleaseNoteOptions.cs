// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Test.Corpus;

/// <summary>
/// Parses the <c>options</c> field of a release-note fix catalogue row, written <c>Name=Value;Name=Value</c>.
/// </summary>
/// <remarks>
/// <para>
/// The field is split on <c>;</c>, and each pair at its first <c>=</c>. A name is an ASCII letter followed by ASCII
/// letters and digits, and may appear once. A value is decoded with <see cref="CorpusEscapes" />, so a semicolon inside
/// a value is written <c>\x3B</c> and a tab <c>\t</c>.
/// </para>
/// <para>
/// The names an area accepts, and what each value means, belong to that area's corpus tests; this parser only reads the
/// field's shape.
/// </para>
/// </remarks>
public static class ReleaseNoteOptions
{
    /// <summary>
    /// Attempts to parse an <c>options</c> field.
    /// </summary>
    /// <param name="text">The field, as it appears in the catalogue; empty for a row that uses the defaults.</param>
    /// <param name="options">
    /// When the method returns <see langword="true" />, the name and decoded value of each option, in the order
    /// written.
    /// </param>
    /// <param name="error">
    /// When the method returns <see langword="false" />, a description of the first problem.
    /// </param>
    /// <returns><see langword="true" /> when the field is well formed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    public static bool TryParse(
        string text,
        [NotNullWhen(true)] out IReadOnlyList<KeyValuePair<string, string>>? options,
        [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(text);

        options = null;
        error = null;

        var parsed = new List<KeyValuePair<string, string>>();
        if (text.Length == 0)
        {
            options = parsed;
            return true;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string pair in text.Split(';'))
        {
            int equals = pair.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0)
            {
                error = $"the option '{pair}' has no '='";
                return false;
            }

            string name = pair[..equals];
            if (!IsName(name))
            {
                error = $"the option name '{name}' is not an ASCII letter followed by letters and digits";
                return false;
            }

            if (!seen.Add(name))
            {
                error = $"the option '{name}' appears more than once";
                return false;
            }

            string value;
            try
            {
                value = CorpusEscapes.DecodeText(pair[(equals + 1)..]);
            }
            catch (FormatException ex)
            {
                error = $"the value of option '{name}' is not valid: {ex.Message}";
                return false;
            }

            parsed.Add(new KeyValuePair<string, string>(name, value));
        }

        options = parsed;
        return true;
    }

    /// <summary>
    /// Parses an <c>options</c> field.
    /// </summary>
    /// <param name="text">The field, as it appears in the catalogue; empty for a row that uses the defaults.</param>
    /// <returns>The name and decoded value of each option, in the order written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException"><paramref name="text" /> is not well formed.</exception>
    public static IReadOnlyList<KeyValuePair<string, string>> Parse(string text) =>
        TryParse(text, out IReadOnlyList<KeyValuePair<string, string>>? options, out string? error)
            ? options
            : throw new FormatException(error);

    /// <summary>
    /// Determines whether text is a valid option name: an ASCII letter followed by ASCII letters and digits.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <returns><see langword="true" /> when <paramref name="name" /> is a valid option name.</returns>
    private static bool IsName(string name)
    {
        if (name.Length == 0 || !char.IsAsciiLetter(name[0]))
            return false;

        foreach (char c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c))
                return false;
        }

        return true;
    }
}
