// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTagLayout.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Identifies the layout strategy applied to an XML documentation tag.
/// </summary>
public enum XmlDocTagLayout
{
    /// <summary>
    /// Chooses the layout from the policy's set membership (block, inline, single-line-when-short).
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Always emit the tag with the opening and closing tags on their own lines.
    /// </summary>
    MultilineBlock = 1,

    /// <summary>
    /// Emit the tag on a single line when its content fits within the maximum single-line length; otherwise expand.
    /// </summary>
    SingleLineWhenShort = 2,

    /// <summary>
    /// Treat the tag as an inline atomic token that must never be split across lines.
    /// </summary>
    InlineAtomic = 3,
}
