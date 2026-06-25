// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlBlockChomping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies how trailing line breaks are handled in a block scalar (the literal <c>|</c> or folded <c>&gt;</c>
/// styles), corresponding to the chomping indicator in the block scalar header.
/// </summary>
/// <remarks>
/// The chomping indicator controls only the final line breaks at the end of the block scalar's content; line breaks
/// within the content follow the literal or folded style rules instead.
/// </remarks>
public enum YamlBlockChomping
{
    /// <summary>
    /// The clip indicator (no character). A single trailing line break is kept, and any further trailing blank lines
    /// are removed. This is the default when no chomping indicator is present.
    /// </summary>
    Clip = 0,

    /// <summary>
    /// The strip indicator (<c>-</c>). All trailing line breaks are removed.
    /// </summary>
    Strip = 1,

    /// <summary>
    /// The keep indicator (<c>+</c>). All trailing line breaks, including blank lines, are preserved.
    /// </summary>
    Keep = 2,
}
