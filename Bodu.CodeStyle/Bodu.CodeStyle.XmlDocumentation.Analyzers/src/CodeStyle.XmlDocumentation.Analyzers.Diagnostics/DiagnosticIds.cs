// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;

/// <summary>
/// Provides the well-known Roslyn diagnostic identifiers reported by this analyzer assembly.
/// </summary>
/// <remarks>
/// Diagnostic IDs follow the <c>BODU####</c> scheme — the thousands digit denotes the analyzer family; the
/// remaining digits identify a specific rule. The XML-documentation family is <c>BODU1xxx</c> and is laid out
/// as follows: <c>BODU1001</c>–<c>BODU1018</c> per-tag formatting; <c>BODU1040</c> cross-cutting formatting;
/// <c>BODU1041</c>–<c>BODU1099</c> reserved for future granular cross-cutting splits; <c>BODU1100</c>+ reserved
/// for required-tag rules; <c>BODU1200</c>+ for ordering; <c>BODU1300</c>+ for cref validity; <c>BODU1400</c>+
/// for content quality. See <c>Bodu.CodeStyle/README.md#diagnostic-ids</c> for the canonical range table.
/// </remarks>
internal static class DiagnosticIds
{
    /// <summary>
    /// The infrastructure diagnostic identifier reported when a <c>bodu.xmldocstyle.json</c> configuration file
    /// is invalid and cannot be applied.
    /// </summary>
    public const string XmlDocConfigInvalid = "BODU0001";

    /// <summary>The diagnostic identifier for <c>&lt;summary&gt;</c> formatting issues.</summary>
    public const string XmlDocSummary = "BODU1001";

    /// <summary>The diagnostic identifier for <c>&lt;remarks&gt;</c> formatting issues.</summary>
    public const string XmlDocRemarks = "BODU1002";

    /// <summary>The diagnostic identifier for <c>&lt;para&gt;</c> formatting issues.</summary>
    public const string XmlDocPara = "BODU1003";

    /// <summary>The diagnostic identifier for <c>&lt;example&gt;</c> formatting issues.</summary>
    public const string XmlDocExample = "BODU1004";

    /// <summary>The diagnostic identifier for <c>&lt;code&gt;</c> formatting issues.</summary>
    public const string XmlDocCode = "BODU1005";

    /// <summary>The diagnostic identifier for <c>&lt;list&gt;</c> formatting issues.</summary>
    public const string XmlDocList = "BODU1006";

    /// <summary>The diagnostic identifier for <c>&lt;item&gt;</c> formatting issues.</summary>
    public const string XmlDocItem = "BODU1007";

    /// <summary>The diagnostic identifier for <c>&lt;description&gt;</c> formatting issues.</summary>
    public const string XmlDocDescription = "BODU1008";

    /// <summary>The diagnostic identifier for <c>&lt;term&gt;</c> formatting issues.</summary>
    public const string XmlDocTerm = "BODU1009";

    /// <summary>The diagnostic identifier for <c>&lt;param&gt;</c> formatting issues.</summary>
    public const string XmlDocParam = "BODU1010";

    /// <summary>The diagnostic identifier for <c>&lt;typeparam&gt;</c> formatting issues.</summary>
    public const string XmlDocTypeParam = "BODU1011";

    /// <summary>The diagnostic identifier for <c>&lt;returns&gt;</c> formatting issues.</summary>
    public const string XmlDocReturns = "BODU1012";

    /// <summary>The diagnostic identifier for <c>&lt;exception&gt;</c> formatting issues.</summary>
    public const string XmlDocException = "BODU1013";

    /// <summary>The diagnostic identifier for <c>&lt;value&gt;</c> formatting issues.</summary>
    public const string XmlDocValue = "BODU1014";

    /// <summary>The diagnostic identifier for <c>&lt;c&gt;</c> inline formatting issues.</summary>
    public const string XmlDocInlineCode = "BODU1015";

    /// <summary>The diagnostic identifier for <c>&lt;see&gt;</c> inline formatting issues.</summary>
    public const string XmlDocSee = "BODU1016";

    /// <summary>The diagnostic identifier for <c>&lt;paramref&gt;</c> inline formatting issues.</summary>
    public const string XmlDocParamRef = "BODU1017";

    /// <summary>The diagnostic identifier for <c>&lt;typeparamref&gt;</c> inline formatting issues.</summary>
    public const string XmlDocTypeParamRef = "BODU1018";

    /// <summary>
    /// The diagnostic identifier reported when the formatter changes documentation prose, prefix, indent, or
    /// any other content that does not belong to a specific tag scope.
    /// </summary>
    public const string XmlDocCrossCutting = "BODU1040";

    /// <summary>
    /// The diagnostic identifier reported when a <c>&lt;code&gt;</c> documentation element does not contain a
    /// <c>&lt;![CDATA[…]]&gt;</c> section as its first non-whitespace child.
    /// </summary>
    public const string XmlDocCodeRequiresCData = "BODU1405";

    /// <summary>
    /// The diagnostic identifier reported when a <c>&lt;typeparam&gt;</c> documentation element carries
    /// explanatory prose that overflows the single-line budget and should be relocated into a
    /// <c>&lt;remarks&gt;&lt;para&gt;…&lt;/para&gt;&lt;/remarks&gt;</c> block.
    /// </summary>
    public const string XmlDocTypeParamRequiresShortContent = "BODU1406";
}
