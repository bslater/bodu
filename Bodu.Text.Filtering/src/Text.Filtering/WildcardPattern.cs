// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Provides the wildcard-pattern engine: brace-alternation expansion, compilation of an expanded pattern into its
/// cost-tier strategy, and the general unit matcher.
/// </summary>
/// <remarks>
/// <para>
/// The supported grammar is the shell-glob core shared by Java's <c>PathMatcher</c> and minimatch, matched against the
/// whole value: <c>*</c> (zero or more characters), <c>?</c> (exactly one character), <c>[abc]</c> / <c>[a-z]</c> /
/// <c>[!abc]</c> character classes, <c>{a,b}</c> alternation, and <c>\</c> escaping the following character. There are
/// no path semantics — <c>*</c> crosses every character equally.
/// </para>
/// <para>
/// Expansion limits (<see cref="MaxBraceNestingDepth" />, <see cref="MaxBraceExpansion" />) mirror the caps used by
/// <c>ConfigurationPattern</c> in <c>Bodu.Text.Configuration</c> and exist to bound the work a hostile pattern can
/// demand at build time.
/// </para>
/// </remarks>
internal static partial class WildcardPattern
{
    /// <summary>The maximum brace-alternation nesting depth permitted in a pattern.</summary>
    internal const int MaxBraceNestingDepth = 32;

    /// <summary>The maximum number of alternatives a pattern may expand into.</summary>
    internal const int MaxBraceExpansion = 10_000;
}
