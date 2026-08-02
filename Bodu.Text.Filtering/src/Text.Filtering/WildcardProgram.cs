// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardProgram.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents one wildcard pattern compiled into its cost-tier strategy: the classification plus the precomputed
/// literal fragments (or the general unit program) that strategy evaluates with.
/// </summary>
/// <remarks>
/// Only the members relevant to <see cref="Kind" /> are populated; the remaining literal fragments are empty strings
/// and <see cref="Units" /> is empty unless the kind is <see cref="WildcardMatchKind.General" />.
/// </remarks>
internal sealed class WildcardProgram
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WildcardProgram" /> class.
    /// </summary>
    /// <param name="kind">The cost-tier strategy the pattern evaluates with.</param>
    /// <param name="literal">The full literal for <see cref="WildcardMatchKind.Literal" /> patterns.</param>
    /// <param name="prefix">The leading literal for prefix-bearing strategies.</param>
    /// <param name="suffix">The trailing literal for suffix-bearing strategies.</param>
    /// <param name="core">The inner literal for <see cref="WildcardMatchKind.Contains" /> patterns.</param>
    /// <param name="units">The unit program for <see cref="WildcardMatchKind.General" /> patterns.</param>
    internal WildcardProgram(WildcardMatchKind kind, string literal, string prefix, string suffix, string core, WildcardUnit[] units)
    {
        Kind = kind;
        Literal = literal;
        Prefix = prefix;
        Suffix = suffix;
        Core = core;
        Units = units;
    }

    /// <summary>
    /// Gets the cost-tier strategy the pattern evaluates with.
    /// </summary>
    public WildcardMatchKind Kind { get; }

    /// <summary>
    /// Gets the full literal text compared by <see cref="WildcardMatchKind.Literal" /> patterns.
    /// </summary>
    public string Literal { get; }

    /// <summary>
    /// Gets the leading literal compared by <see cref="WildcardMatchKind.Prefix" /> and
    /// <see cref="WildcardMatchKind.PrefixAndSuffix" /> patterns.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Gets the trailing literal compared by <see cref="WildcardMatchKind.Suffix" /> and
    /// <see cref="WildcardMatchKind.PrefixAndSuffix" /> patterns.
    /// </summary>
    public string Suffix { get; }

    /// <summary>
    /// Gets the inner literal searched for by <see cref="WildcardMatchKind.Contains" /> patterns.
    /// </summary>
    public string Core { get; }

    /// <summary>
    /// Gets the compiled unit program executed by <see cref="WildcardMatchKind.General" /> patterns.
    /// </summary>
    public WildcardUnit[] Units { get; }

    /// <summary>
    /// Determines whether the specified value matches this compiled pattern.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="comparison">
    /// The comparison applied to the literal-fragment strategies; must be <see cref="StringComparison.Ordinal" /> or
    /// <see cref="StringComparison.OrdinalIgnoreCase" />.
    /// </param>
    /// <param name="ignoreCase">Whether the general unit matcher folds case.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> matches the pattern; otherwise <see langword="false" />.
    /// </returns>
    public bool IsMatch(ReadOnlySpan<char> value, StringComparison comparison, bool ignoreCase) =>
        Kind switch
        {
            WildcardMatchKind.MatchAll => true,
            WildcardMatchKind.Literal => value.Equals(Literal, comparison),
            WildcardMatchKind.Prefix => value.Length >= Prefix.Length && value.StartsWith(Prefix, comparison),
            WildcardMatchKind.Suffix => value.EndsWith(Suffix, comparison),
            WildcardMatchKind.PrefixAndSuffix => value.Length >= Prefix.Length + Suffix.Length
                && value.StartsWith(Prefix, comparison)
                && value.EndsWith(Suffix, comparison),
            WildcardMatchKind.Contains => value.Contains(Core, comparison),
            _ => WildcardPattern.IsMatch(value, Units, ignoreCase),
        };
}
