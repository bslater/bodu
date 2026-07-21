// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvLimits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Defines hard, non-configurable limits that bound resource use while parsing and writing DotEnv, independent of any
/// caller-supplied option.
/// </summary>
internal static class DotEnvLimits
{
    /// <summary>The maximum number of bytes a single entry (its key, assignment, and value) may occupy before it resolves while reading a multi-segment stream.</summary>
    /// <remarks>
    /// The whole-buffer reader parses in place and is not exposed to this bound; the resumable streaming path uses it
    /// to cap the work an oversized or unterminated construct can force before the entry is completed, failing with a
    /// catchable <see cref="DotEnvFormatException" /> rather than accumulating without limit.
    /// </remarks>
    internal const int MaxEntryLength = 1 << 20;
}
