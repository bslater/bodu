// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Provides the per-comment information that <see cref="XmlDocFormatter" /> needs to reproduce the original
/// indentation, line endings, and member context.
/// </summary>
public sealed class XmlDocFormatContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocFormatContext" /> class.
    /// </summary>
    /// <param name="baseIndent">The whitespace that precedes each <c>///</c> line in the original source.</param>
    /// <param name="lineEnding">The line ending sequence used in the source file.</param>
    /// <param name="memberKindHint">A hint identifying the kind of member that owns the documentation comment.</param>
    public XmlDocFormatContext(
        string baseIndent,
        string lineEnding,
        XmlDocMemberKindHint memberKindHint)
    {
        if (baseIndent is null) throw new ArgumentNullException(nameof(baseIndent));
        if (lineEnding is null) throw new ArgumentNullException(nameof(lineEnding));
        if (lineEnding.Length == 0) throw new ArgumentException(XmlDocResourceStrings.Arg_Invalid_LineEndingEmpty, nameof(lineEnding));

        this.BaseIndent = baseIndent;
        this.LineEnding = lineEnding;
        this.MemberKindHint = memberKindHint;
    }

    /// <summary>
    /// Gets the whitespace prefix that appears before each documentation line in the original source.
    /// </summary>
    /// <value>The base indent string captured from the source.</value>
    public string BaseIndent { get; }

    /// <summary>
    /// Gets the line ending sequence that should be used for every emitted physical line.
    /// </summary>
    /// <value>The line ending sequence; typically <c>"\r\n"</c> or <c>"\n"</c>.</value>
    public string LineEnding { get; }

    /// <summary>
    /// Gets a hint identifying the kind of member that owns the documentation comment.
    /// </summary>
    /// <value>The member-kind hint reported by the caller.</value>
    public XmlDocMemberKindHint MemberKindHint { get; }
}
