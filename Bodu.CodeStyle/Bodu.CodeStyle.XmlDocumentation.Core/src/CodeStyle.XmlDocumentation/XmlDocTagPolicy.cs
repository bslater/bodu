// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTagPolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Describes per-tag layout policy overrides used by <see cref="XmlDocFormatter" /> to decide whether a tag is
/// emitted block-style, single-line, or inline.
/// </summary>
public sealed class XmlDocTagPolicy
{
    /// <summary>
    /// Gets the default policy applied when a tag has no explicit override.
    /// </summary>
    /// <value>A singleton default policy with all overrides unset.</value>
    public static XmlDocTagPolicy Default { get; } = new XmlDocTagPolicy(
        XmlDocTagLayout.Auto,
        maxSingleLineLength: null,
        allowLineBreakInside: null,
        selfClosingTrailingSpace: null);

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocTagPolicy" /> class.
    /// </summary>
    /// <param name="layout">The preferred layout for the tag.</param>
    /// <param name="maxSingleLineLength">An optional override for the maximum length to keep the tag single-line.</param>
    /// <param name="allowLineBreakInside">An optional override controlling whether line breaks may appear inside the tag content.</param>
    /// <param name="selfClosingTrailingSpace">An optional override controlling the space before <c>/&gt;</c> on self-closing forms.</param>
    public XmlDocTagPolicy(
        XmlDocTagLayout layout,
        int? maxSingleLineLength,
        bool? allowLineBreakInside,
        bool? selfClosingTrailingSpace)
    {
        this.Layout = layout;
        this.MaxSingleLineLength = maxSingleLineLength;
        this.AllowLineBreakInside = allowLineBreakInside;
        this.SelfClosingTrailingSpace = selfClosingTrailingSpace;
    }

    /// <summary>
    /// Gets the preferred layout for the tag.
    /// </summary>
    /// <value>The configured layout choice.</value>
    public XmlDocTagLayout Layout { get; }

    /// <summary>
    /// Gets the maximum length, in characters, that allows the tag to remain on a single line.
    /// </summary>
    /// <value>The override length when configured; otherwise <see langword="null" />.</value>
    public int? MaxSingleLineLength { get; }

    /// <summary>
    /// Gets a value indicating whether line breaks may appear inside the tag content.
    /// </summary>
    /// <value><see langword="true" /> when wrapping is permitted; <see langword="false" /> when forbidden; <see langword="null" /> when not overridden.</value>
    public bool? AllowLineBreakInside { get; }

    /// <summary>
    /// Gets a value indicating whether a single space should appear before the closing <c>/&gt;</c> of a
    /// self-closing form.
    /// </summary>
    /// <value><see langword="true" /> to emit the space; <see langword="false" /> to suppress it; <see langword="null" /> to preserve as-written.</value>
    public bool? SelfClosingTrailingSpace { get; }
}
