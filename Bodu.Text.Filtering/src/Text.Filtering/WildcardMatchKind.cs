// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardMatchKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Identifies the cost-tier strategy a compiled wildcard pattern evaluates with, ordered cheapest first.
/// </summary>
internal enum WildcardMatchKind
{
    /// <summary>
    /// The pattern is a lone <c>*</c> and matches every value.
    /// </summary>
    MatchAll = 0,

    /// <summary>
    /// The pattern contains no metacharacters and matches by whole-string equality.
    /// </summary>
    Literal = 1,

    /// <summary>
    /// The pattern is <c>literal*</c> and matches by prefix comparison.
    /// </summary>
    Prefix = 2,

    /// <summary>
    /// The pattern is <c>*literal</c> and matches by suffix comparison.
    /// </summary>
    Suffix = 3,

    /// <summary>
    /// The pattern is <c>literal*literal</c> and matches by non-overlapping prefix and suffix comparisons.
    /// </summary>
    PrefixAndSuffix = 4,

    /// <summary>
    /// The pattern is <c>*literal*</c> and matches by substring search.
    /// </summary>
    Contains = 5,

    /// <summary>
    /// The pattern requires the general wildcard matcher (it contains <c>?</c>, character classes, or multiple
    /// interleaved <c>*</c> segments).
    /// </summary>
    General = 6,
}
