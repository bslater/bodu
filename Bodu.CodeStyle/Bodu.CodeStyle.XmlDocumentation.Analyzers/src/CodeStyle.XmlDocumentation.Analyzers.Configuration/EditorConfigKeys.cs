// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EditorConfigKeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;

/// <summary>
/// Provides the well-known <c>.editorconfig</c> keys consumed by the XML documentation formatter.
/// </summary>
internal static class EditorConfigKeys
{
    /// <summary>The <c>.editorconfig</c> key controlling the maximum documentation line length.</summary>
    public const string MaxLineLength = "bodu_xmldoc_max_line_length";

    /// <summary>The <c>.editorconfig</c> key controlling the indent size (number of spaces).</summary>
    public const string IndentSize = "bodu_xmldoc_indent_size";

    /// <summary>The <c>.editorconfig</c> key controlling whether inline tags are preserved.</summary>
    public const string PreserveInlineTags = "bodu_xmldoc_preserve_inline_tags";

    /// <summary>The <c>.editorconfig</c> key controlling whether <c>&lt;para&gt;</c> is forced multiline.</summary>
    public const string ForceParaMultiline = "bodu_xmldoc_force_para_multiline";

    /// <summary>The <c>.editorconfig</c> key controlling whether <c>&lt;summary&gt;</c> is forced multiline.</summary>
    public const string ForceSummaryMultiline = "bodu_xmldoc_force_summary_multiline";

    /// <summary>The standard line-ending key from <c>.editorconfig</c>.</summary>
    public const string EndOfLine = "end_of_line";
}
