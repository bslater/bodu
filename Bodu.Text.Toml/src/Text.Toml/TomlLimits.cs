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
    /// <see cref="StackOverflowException" />, which cannot be caught. This ceiling guarantees that excessive nesting
    /// instead fails with a catchable <see cref="TomlFormatException" /> (when reading) or
    /// <see cref="TomlSerializationException" /> (when writing), which is essential when processing untrusted input.
    /// </para>
    /// <para>
    /// The value must stay small enough that the ceiling is reached before the physical call stack is exhausted;
    /// otherwise the recursive serializer overflows the stack first and the guarantee is lost. The serializer's
    /// per-level recursion consumes roughly a kilobyte of stack, so a ceiling near a thousand sits at the edge of the
    /// common 1 MB thread stack (the default on Windows) and overflows before the guard can throw. This value keeps
    /// the bounded recursion comfortably within a modest stack budget — well under 1 MB — while remaining far deeper
    /// than any realistic document: it is four times the 64-level serializer default and equal to the 256-level
    /// ceiling the reader and writer already apply by default.
    /// </para>
    /// </remarks>
    internal const int AbsoluteMaxDepth = 256;
}
