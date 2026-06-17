// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLimits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Defines hard, non-configurable limits that bound resource use while parsing and writing TOML, independent of any
/// caller-supplied option.
/// </summary>
internal static class TomlLimits
{
    /// <summary>The absolute maximum container nesting depth enforced while parsing or writing, applied even when a caller configures a larger maximum depth.</summary>
    /// <remarks>
    /// <para>
    /// A configurable maximum depth only bounds normal use; left unbounded, a caller could set an arbitrarily large
    /// value and a deeply nested document or object graph would recurse until the process terminated with a
    /// <see cref="StackOverflowException" />, which cannot be caught. Every caller-supplied maximum depth is therefore
    /// clamped to this ceiling, so excessive nesting fails with a catchable <see cref="TomlFormatException" /> (when
    /// reading) or <see cref="TomlSerializationException" /> (when writing) — essential when processing untrusted
    /// input.
    /// </para>
    /// <para>
    /// The reader, writer, and serializer descend one native call-stack frame per nested container, so the ceiling must
    /// be reached before the physical stack is exhausted; otherwise the recursion overflows first and the guarantee is
    /// lost. The value therefore matches the <see cref="TomlSerializerOptions.DefaultMaxDepth" /> of 64 — deep enough
    /// for any realistic document, yet shallow enough that the bounded recursion stays well within a modest stack
    /// budget even when much of the stack was already consumed before the call (for example under a deep asynchronous
    /// call chain). Supporting depths beyond this safely would require replacing the recursive descent with an explicit
    /// stack-based traversal; until then a low ceiling is the deliberate trade-off.
    /// </para>
    /// </remarks>
    internal const int AbsoluteMaxDepth = TomlSerializerOptions.DefaultMaxDepth;
}
