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
    /// <summary>
    /// The absolute maximum container nesting depth enforced while parsing or writing, applied even when a caller
    /// configures a larger maximum depth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A configurable maximum depth only bounds normal use; left unbounded, a caller could set an arbitrarily large
    /// value and a deeply nested document or object graph would recurse until the process terminated with a
    /// <see cref="StackOverflowException" />, which cannot be caught. This ceiling caps the configured depth so excessive
    /// nesting fails with a catchable <see cref="TomlFormatException" /> (when reading) or
    /// <see cref="TomlSerializationException" /> (when writing), which is essential when processing untrusted input.
    /// </para>
    /// <para>
    /// This constant is a <em>logical</em> bound only; it is not what guarantees the process survives. The amount of
    /// stack already consumed before serialization begins varies with the call context, so no fixed level count can be
    /// chosen that is always reached before the physical stack is exhausted — a depth that is safe on a fresh thread can
    /// overflow under a deep asynchronous call chain. The physical guarantee instead comes from probing the actual
    /// remaining call stack on each descent (see the writer's stack guard), which converts impending exhaustion into a
    /// catchable exception regardless of platform, thread stack size, or call context. This value sits far deeper than
    /// any realistic document — four times the 64-level serializer default, and equal to the 256-level ceiling the reader
    /// and writer apply by default — so it bounds pathological configured depths without constraining legitimate use.
    /// </para>
    /// </remarks>
    internal const int AbsoluteMaxDepth = 256;
}
