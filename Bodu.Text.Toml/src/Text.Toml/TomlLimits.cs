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
    /// A configurable maximum depth only bounds normal use; left unbounded, a caller could set an arbitrarily large
    /// value and a deeply nested document or object graph would recurse until the process terminated with a
    /// <see cref="StackOverflowException" />, which cannot be caught. This ceiling guarantees that excessive nesting
    /// instead fails with a catchable <see cref="TomlFormatException" /> (when reading) or
    /// <see cref="TomlSerializationException" /> (when writing), which is essential when processing untrusted input.
    /// </remarks>
    internal const int AbsoluteMaxDepth = 1024;
}
