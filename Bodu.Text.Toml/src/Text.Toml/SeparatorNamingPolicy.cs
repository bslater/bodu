// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SeparatorNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml;

/// <summary>
/// A naming policy that converts a <c>PascalCase</c> or <c>camelCase</c> member name to a separated form, inserting a
/// separator character at word boundaries and applying a uniform letter case. Backs the snake-case and kebab-case
/// policies.
/// </summary>
/// <remarks>
/// A boundary is detected before an uppercase letter that follows a lowercase letter or digit, and before an uppercase
/// letter that begins a new word at the end of an acronym (an uppercase letter followed by a lowercase letter). The
/// result is then lowercased or uppercased uniformly.
/// </remarks>
internal sealed class SeparatorNamingPolicy
    : TomlNamingPolicy
{
    /// <summary>
    /// The character inserted at word boundaries.
    /// </summary>
    private readonly char _separator;

    /// <summary>
    /// Indicates whether the result is uppercased; otherwise it is lowercased.
    /// </summary>
    private readonly bool _toUpper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeparatorNamingPolicy" /> class.
    /// </summary>
    /// <param name="separator">The character inserted at word boundaries.</param>
    /// <param name="toUpper">When <see langword="true" /> the result is uppercased; otherwise it is lowercased.</param>
    internal SeparatorNamingPolicy(char separator, bool toUpper)
    {
        _separator = separator;
        _toUpper = toUpper;
    }

    /// <inheritdoc />
    public override string ConvertName(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        if (name.Length == 0)
            return name;

        StringBuilder builder = new(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0 && IsBoundary(name, i))
                builder.Append(_separator);

            builder.Append(_toUpper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether a word boundary falls immediately before the uppercase letter at <paramref name="index" />.
    /// </summary>
    /// <param name="name">The name being converted.</param>
    /// <param name="index">The index of the uppercase letter under test.</param>
    /// <returns>
    /// <see langword="true" /> when a separator should precede the letter; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsBoundary(string name, int index)
    {
        char previous = name[index - 1];

        // A lowercase or digit immediately before an uppercase letter starts a new word (e.g. "fooBar" -> "foo|Bar").
        if (!char.IsUpper(previous))
            return true;

        // The tail of an acronym before a new word: an uppercase run followed by a lowercase letter (e.g. "HTTPServer" -> "HTTP|Server").
        return index + 1 < name.Length && char.IsLower(name[index + 1]);
    }
}
