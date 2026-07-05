// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlLimits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Defines hard, non-configurable limits that bound resource use while parsing and writing YAML, independent of any
/// caller-supplied option.
/// </summary>
internal static class YamlLimits
{
    /// <summary>The default container nesting depth bound applied when a caller does not configure one.</summary>
    internal const int DefaultMaxDepth = 64;

    /// <summary>The absolute maximum container nesting depth enforced while parsing or writing, applied even when a caller configures a larger maximum depth.</summary>
    /// <remarks>
    /// <para>
    /// A configurable maximum depth only bounds normal use; left unbounded, a caller could set an arbitrarily large
    /// value and a deeply nested document or object graph would recurse until the process terminated with a
    /// <see cref="StackOverflowException" />, which cannot be caught. Every caller-supplied maximum depth is therefore
    /// clamped to this ceiling, so excessive nesting fails with a catchable <see cref="YamlFormatException" /> (when
    /// reading) or <see cref="YamlSerializationException" /> (when writing) — essential when processing untrusted
    /// input.
    /// </para>
    /// <para>
    /// The reader, writer, and serializer descend one native call-stack frame per nested container, so the ceiling must
    /// be reached before the physical stack is exhausted; otherwise the recursion overflows first and the guarantee is
    /// lost. The value therefore matches <see cref="DefaultMaxDepth" /> — deep enough for any realistic document, yet
    /// shallow enough that the bounded recursion stays well within a modest stack budget even when much of the stack
    /// was already consumed before the call. Supporting depths beyond this safely would require replacing the recursive
    /// descent with an explicit stack-based traversal; until then a low ceiling is the deliberate trade-off.
    /// </para>
    /// </remarks>
    internal const int AbsoluteMaxDepth = DefaultMaxDepth;

    /// <summary>The absolute maximum number of nodes a document may expand to once aliases are resolved.</summary>
    /// <remarks>
    /// Aliases share a single physical subtree, so the parsed row graph stays small, but a consumer that
    /// materializes the graph into a tree expands each alias into a full copy of its target. Chained aliases
    /// (<c>b: [*a, *a]</c>, <c>c: [*b, *b]</c>, …) therefore expand exponentially — the classic "billion laughs"
    /// amplification. Bounding the total expanded node count converts that amplification into a catchable
    /// <see cref="YamlFormatException" /> before any consumer can exhaust memory.
    /// </remarks>
    internal const long AbsoluteMaxExpandedNodes = 10_000_000;
}
