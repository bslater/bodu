// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.CompiledPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Bodu.Text.Filtering;

/// <content> The compiled per-pattern entry evaluated on the hot path. </content>
public sealed partial class TextFilter
{
    /// <summary>
    /// Represents one compiled matcher: a wildcard program or a regular expression, together with its resolved
    /// comparison, action, cost tier, and the declaration index of the pattern it came from.
    /// </summary>
    private readonly struct CompiledPattern
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledPattern" /> struct.
        /// </summary>
        /// <param name="wildcard">
        /// The compiled wildcard program, or <see langword="null" /> for a regular expression.
        /// </param>
        /// <param name="regex">The compiled regular expression, or <see langword="null" /> for a wildcard.</param>
        /// <param name="ignoreCase">Whether the pattern matches case-insensitively.</param>
        /// <param name="isExclude">Whether the pattern's action is <see cref="TextFilterAction.Exclude" />.</param>
        /// <param name="sourceIndex">The declaration index of the pattern this entry was compiled from.</param>
        public CompiledPattern(WildcardProgram? wildcard, Regex? regex, bool ignoreCase, bool isExclude, int sourceIndex)
        {
            Wildcard = wildcard;
            Regex = regex;
            IgnoreCase = ignoreCase;
            Comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            IsExclude = isExclude;
            SourceIndex = sourceIndex;
            CostTier = wildcard is null
                ? RegexCostTier
                : wildcard.Kind switch
                {
                    WildcardMatchKind.MatchAll => 0,
                    WildcardMatchKind.Literal => 1,
                    WildcardMatchKind.Prefix or WildcardMatchKind.Suffix or WildcardMatchKind.PrefixAndSuffix => 2,
                    WildcardMatchKind.Contains => 3,
                    _ => 4,
                };
        }

        /// <summary>The cost tier assigned to regular-expression entries — always the most expensive.</summary>
        private const int RegexCostTier = 5;

        /// <summary>
        /// Gets the compiled wildcard program, or <see langword="null" /> when this entry is a regular expression.
        /// </summary>
        public WildcardProgram? Wildcard { get; }

        /// <summary>
        /// Gets the compiled regular expression, or <see langword="null" /> when this entry is a wildcard.
        /// </summary>
        public Regex? Regex { get; }

        /// <summary>
        /// Gets a value indicating whether the pattern matches case-insensitively.
        /// </summary>
        public bool IgnoreCase { get; }

        /// <summary>
        /// Gets the string comparison applied by the wildcard literal-fragment strategies.
        /// </summary>
        public StringComparison Comparison { get; }

        /// <summary>
        /// Gets a value indicating whether the pattern's action is <see cref="TextFilterAction.Exclude" />.
        /// </summary>
        public bool IsExclude { get; }

        /// <summary>
        /// Gets the declaration index of the pattern this entry was compiled from — the slot it addresses in the
        /// declared-pattern array and the hit-count array.
        /// </summary>
        public int SourceIndex { get; }

        /// <summary>
        /// Gets the evaluation cost tier used to order entries cheapest-first.
        /// </summary>
        public int CostTier { get; }

        /// <summary>
        /// Determines whether the specified value matches this compiled entry.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="timeoutCount">Incremented when the regular-expression engine times out on this entry.</param>
        /// <returns><see langword="true" /> when the value matches; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// A regular-expression timeout is decided fail-safe: a timed-out include reports no match (the value is not
        /// admitted by it), while a timed-out exclude reports a match (the value is still rejected).
        /// </remarks>
        public bool IsMatch(ReadOnlySpan<char> value, ref int timeoutCount)
        {
            if (Wildcard is not null)
                return Wildcard.IsMatch(value, Comparison, IgnoreCase);

            try
            {
                return Regex!.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                timeoutCount++;
                return IsExclude;
            }
        }
    }
}
