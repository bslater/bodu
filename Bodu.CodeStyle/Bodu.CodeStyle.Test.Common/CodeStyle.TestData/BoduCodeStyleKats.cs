// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduCodeStyleKats.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bodu.CodeStyle.TestData;

/// <summary>
/// Identifies the broad feature area covered by a known-answer test case.
/// </summary>
public enum CodeStyleKatArea
{
    XmlDocumentationFormatting,
    XmlDocumentationAnalyzer,
    XmlDocumentationCodeFix,
    Configuration,
    Safety,
}

/// <summary>
/// Identifies the operation expected by a known-answer test case.
/// </summary>
public enum CodeStyleKatOperation
{
    FormatTrivia,
    AnalyzeDocument,
    ApplyCodeFix,
    ApplyFixAll,
}

/// <summary>
/// Describes the expected Fix All scope for a code-fix KAT.
/// </summary>
public enum CodeStyleKatFixAllScope
{
    None,
    SingleDiagnostic,
    Document,
    Project,
    Solution,
}

/// <summary>
/// Describes expected formatter failure classifications for malformed or unsupported input.
/// </summary>
public enum CodeStyleKatFailureKind
{
    None,
    MalformedXml,
    UnsupportedDocumentationTrivia,
    UnsupportedPreprocessorLayout,
    InvalidConfiguration,
}

/// <summary>
/// Represents a diagnostic expected from a Roslyn analyzer test.
/// </summary>
/// <param name="Id">The diagnostic identifier.</param>
/// <param name="Severity">The expected severity name.</param>
/// <param name="Line">The expected one-based line number, if known.</param>
/// <param name="Column">The expected one-based column number, if known.</param>
/// <param name="MessageContains">Optional text expected in the diagnostic message.</param>
public sealed record CodeStyleKatDiagnostic(
    string Id,
    string Severity = "Warning",
    int? Line = null,
    int? Column = null,
    string? MessageContains = null);

/// <summary>
/// Represents an additional file supplied to analyzer or code-fix tests.
/// </summary>
/// <param name="Path">The file path visible to the analyzer.</param>
/// <param name="Content">The additional file content.</param>
public sealed record CodeStyleKatAdditionalFile(string Path, string Content);

/// <summary>
/// Represents a single known-answer test case for Bodu code-style analyzers, formatters, and code fixes.
/// </summary>
/// <param name="Id">The stable KAT identifier.</param>
/// <param name="Name">The human-readable KAT name.</param>
/// <param name="Area">The feature area covered by the KAT.</param>
/// <param name="Operation">The operation being validated.</param>
/// <param name="Input">The input trivia or full source text.</param>
/// <param name="Expected">The expected output trivia or full source text.</param>
/// <param name="ExpectedDiagnostics">The diagnostics expected from analyzer execution.</param>
/// <param name="AnalyzerConfig">Optional .editorconfig/analyzer config content.</param>
/// <param name="AdditionalFiles">Optional analyzer additional files, such as JSON policy files.</param>
/// <param name="FilePath">The source file path used by analyzer tests.</param>
/// <param name="ExpectedChanged">Whether the formatter/code fix is expected to change the input.</param>
/// <param name="ExpectedFailure">The expected failure classification, if any.</param>
/// <param name="FixAllScope">The expected Fix All scope, if applicable.</param>
/// <param name="ShouldOfferFix">Whether a code fix should be offered for the diagnostic.</param>
/// <param name="Notes">Optional implementation notes for the test.</param>
public sealed record CodeStyleKat(
    string Id,
    string Name,
    CodeStyleKatArea Area,
    CodeStyleKatOperation Operation,
    string Input,
    string Expected,
    IReadOnlyList<CodeStyleKatDiagnostic>? ExpectedDiagnostics = null,
    string? AnalyzerConfig = null,
    IReadOnlyList<CodeStyleKatAdditionalFile>? AdditionalFiles = null,
    string FilePath = "Test0.cs",
    bool ExpectedChanged = true,
    CodeStyleKatFailureKind ExpectedFailure = CodeStyleKatFailureKind.None,
    CodeStyleKatFixAllScope FixAllScope = CodeStyleKatFixAllScope.None,
    bool ShouldOfferFix = true,
    string? Notes = null)
{
    /// <summary>
    /// Gets the expected diagnostics as a non-null read-only list.
    /// </summary>
    public IReadOnlyList<CodeStyleKatDiagnostic> Diagnostics { get; } =
        ExpectedDiagnostics ?? [];

    /// <summary>
    /// Gets the additional files as a non-null read-only list.
    /// </summary>
    public IReadOnlyList<CodeStyleKatAdditionalFile> Files { get; } =
        AdditionalFiles ?? [];

    /// <summary>
    /// Creates a formatter KAT for a single XML documentation trivia block.
    /// </summary>
    public static CodeStyleKat XmlFormat(
        string id,
        string name,
        string input,
        string expected,
        bool expectedChanged = true,
        CodeStyleKatFailureKind expectedFailure = CodeStyleKatFailureKind.None) =>
        new(
            Id: id,
            Name: name,
            Area: CodeStyleKatArea.XmlDocumentationFormatting,
            Operation: CodeStyleKatOperation.FormatTrivia,
            Input: input,
            Expected: expected,
            ExpectedChanged: expectedChanged,
            ExpectedFailure: expectedFailure);

    /// <summary>
    /// Creates an analyzer KAT for a full C# source document.
    /// </summary>
    public static CodeStyleKat Analyze(
        string id,
        string name,
        string source,
        IReadOnlyList<CodeStyleKatDiagnostic> diagnostics,
        string? analyzerConfig = null,
        IReadOnlyList<CodeStyleKatAdditionalFile>? additionalFiles = null,
        string filePath = "Test0.cs") =>
        new(
            Id: id,
            Name: name,
            Area: CodeStyleKatArea.XmlDocumentationAnalyzer,
            Operation: CodeStyleKatOperation.AnalyzeDocument,
            Input: source,
            Expected: source,
            ExpectedDiagnostics: diagnostics,
            AnalyzerConfig: analyzerConfig,
            AdditionalFiles: additionalFiles,
            FilePath: filePath,
            ExpectedChanged: false);

    /// <summary>
    /// Creates a code-fix KAT for a full C# source document.
    /// </summary>
    public static CodeStyleKat CodeFix(
        string id,
        string name,
        string inputSource,
        string expectedSource,
        CodeStyleKatFixAllScope fixAllScope = CodeStyleKatFixAllScope.SingleDiagnostic,
        string? analyzerConfig = null,
        IReadOnlyList<CodeStyleKatAdditionalFile>? additionalFiles = null,
        string filePath = "Test0.cs",
        bool shouldOfferFix = true) =>
        new(
            Id: id,
            Name: name,
            Area: CodeStyleKatArea.XmlDocumentationCodeFix,
            Operation: fixAllScope == CodeStyleKatFixAllScope.SingleDiagnostic
                ? CodeStyleKatOperation.ApplyCodeFix
                : CodeStyleKatOperation.ApplyFixAll,
            Input: inputSource,
            Expected: expectedSource,
            AnalyzerConfig: analyzerConfig,
            AdditionalFiles: additionalFiles,
            FilePath: filePath,
            ExpectedChanged: inputSource != expectedSource,
            FixAllScope: fixAllScope,
            ShouldOfferFix: shouldOfferFix);

    /// <summary>
    /// Creates a configuration KAT for a full C# source document.
    /// </summary>
    public static CodeStyleKat Configuration(
        string id,
        string name,
        string inputSource,
        string expectedSource,
        string? analyzerConfig = null,
        IReadOnlyList<CodeStyleKatAdditionalFile>? additionalFiles = null) =>
        new(
            Id: id,
            Name: name,
            Area: CodeStyleKatArea.Configuration,
            Operation: CodeStyleKatOperation.ApplyCodeFix,
            Input: inputSource,
            Expected: expectedSource,
            AnalyzerConfig: analyzerConfig,
            AdditionalFiles: additionalFiles,
            ExpectedChanged: inputSource != expectedSource,
            FixAllScope: CodeStyleKatFixAllScope.SingleDiagnostic);

    /// <summary>
    /// Creates a safety KAT for a full C# source document.
    /// </summary>
    public static CodeStyleKat Safety(
        string id,
        string name,
        string inputSource,
        string expectedSource,
        CodeStyleKatFailureKind expectedFailure = CodeStyleKatFailureKind.None,
        string filePath = "Test0.cs") =>
        new(
            Id: id,
            Name: name,
            Area: CodeStyleKatArea.Safety,
            Operation: CodeStyleKatOperation.ApplyCodeFix,
            Input: inputSource,
            Expected: expectedSource,
            FilePath: filePath,
            ExpectedChanged: inputSource != expectedSource,
            ExpectedFailure: expectedFailure,
            FixAllScope: CodeStyleKatFixAllScope.SingleDiagnostic,
            ShouldOfferFix: expectedFailure == CodeStyleKatFailureKind.None);

    /// <summary>
    /// Returns a display name suitable for MSTest dynamic data output.
    /// </summary>
    public override string ToString() => $"{Id}: {Name}";
}

/// <summary>
/// Provides shared diagnostic identifiers and default analyzer policy content used by KAT fixtures.
/// </summary>
public static class CodeStyleKatConstants
{
    /// <summary>Diagnostic id surfaced when a <c>&lt;summary&gt;</c> tag's formatting differs from policy.</summary>
    public const string XmlDocSummaryDiagnosticId = "BODU1001";

    /// <summary>Diagnostic id surfaced when an inline <c>&lt;c&gt;</c> tag's formatting differs from policy.</summary>
    public const string XmlDocInlineCodeDiagnosticId = "BODU1015";

    /// <summary>Diagnostic id surfaced for cross-cutting changes outside any tag scope.</summary>
    public const string XmlDocCrossCuttingDiagnosticId = "BODU1040";

    /// <summary>
    /// Legacy alias used by pre-rename fixtures. Equal to <see cref="XmlDocSummaryDiagnosticId" /> because the
    /// renamed <c>BODU1001</c> identifier now denotes the per-tag <c>&lt;summary&gt;</c> rule.
    /// </summary>
    public const string XmlDocumentationDiagnosticId = XmlDocSummaryDiagnosticId;

    public const string DefaultAnalyzerConfig = """
        root = true

        [*.cs]
        dotnet_analyzer_diagnostic.category-Documentation.severity = warning

        bodu_xmldoc_max_line_length = 120
        bodu_xmldoc_force_summary_multiline = true
        bodu_xmldoc_force_para_multiline = true
        bodu_xmldoc_preserve_inline_tags = true
        """;

    public const string DefaultXmlDocumentationPolicyJson = """
        {
          "profile": "Bodu",
          "maxLineLength": 120,
          "collapseProseWhitespace": true,
          "preserveBlankLines": false,
          "preserveXmlTagAttributes": false,
          "preserveCrefText": true,
          "blockTags": [ "summary", "remarks", "para", "example", "list", "item", "description", "code" ],
          "inlineTags": [ "c", "see", "paramref", "typeparamref" ],
          "forceMultilineTags": [ "summary", "remarks", "para", "example", "list" ],
          "singleLineWhenShort": [ "param", "typeparam", "returns", "exception", "value" ],
          "neverSplitTagContent": [ "c", "see", "paramref", "typeparamref" ]
        }
        """;
}

/// <summary>
/// Provides known-answer test fixtures for the Bodu code-style analyzer and formatter suite.
/// </summary>
public static class BoduCodeStyleKats
{
    /// <summary>
    /// Gets all known-answer test fixtures.
    /// </summary>
    public static IReadOnlyList<CodeStyleKat> All { get; } = new List<CodeStyleKat>
    {
        // XML documentation formatter KATs.
        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0001",
            name: "Single-line summary expands to multiline",
            input:
            """
            /// <summary>Parses a DotEnv document.</summary>
            """,
            expected:
            """
            /// <summary>
            /// Parses a DotEnv document.
            /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0002",
            name: "Already formatted summary remains unchanged",
            input:
            """
            /// <summary>
            /// Parses a DotEnv document.
            /// </summary>
            """,
            expected:
            """
            /// <summary>
            /// Parses a DotEnv document.
            /// </summary>
            """,
            expectedChanged: false),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0003",
            name: "Remarks para expands to multiline block",
            input:
            """
            /// <remarks>
            /// <para>Comments and blank lines are not preserved.</para>
            /// </remarks>
            """,
            expected:
            """
            /// <remarks>
            /// <para>
            /// Comments and blank lines are not preserved.
            /// </para>
            /// </remarks>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0004",
            name: "Multiple para tags each expand independently",
            input:
            """
            /// <remarks>
            /// <para>First paragraph.</para> <para>Second paragraph.</para>
            /// </remarks>
            """,
            expected:
            """
            /// <remarks>
            /// <para>
            /// First paragraph.
            /// </para>
            /// <para>
            /// Second paragraph.
            /// </para>
            /// </remarks>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0005",
            name: "Inline c tag remains atomic",
            input:
            """
            /// <summary>Values matching <c>[A-Za-z0-9_.,:/@+\-]</c> are unquoted.</summary>
            """,
            expected:
            """
            /// <summary>
            /// Values matching <c>[A-Za-z0-9_.,:/@+\-]</c> are unquoted.
            /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0006",
            name: "Long inline c token is not split even if line exceeds the limit",
            input:
            """
            /// <summary>Values matching <c>[ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.,:/@+\-]</c> are unquoted.</summary>
            """,
            expected:
            """
            /// <summary>
            /// Values matching <c>[ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.,:/@+\-]</c> are unquoted.
            /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0007",
            name: "See cref tag remains inline and attributes are preserved",
            input:
            """
            /// <summary>Returns a <see cref="DotEnvDocument" /> instance.</summary>
            """,
            expected:
            """
            /// <summary>
            /// Returns a <see cref="DotEnvDocument" /> instance.
            /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0008",
            name: "See langword return tag remains single-line",
            input:
            """
            /// <returns><see langword="true" /> when parsing succeeds; otherwise, <see langword="false" />.</returns>
            """,
            expected:
            """
            /// <returns><see langword="true" /> when parsing succeeds; otherwise, <see langword="false" />.</returns>
            """,
            expectedChanged: false),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0009",
            name: "Short param remains single-line",
            input:
            """
            /// <param name="source">The source text.</param>
            """,
            expected:
            """
            /// <param name="source">The source text.</param>
            """,
            expectedChanged: false),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0010",
            name: "Long param expands to multiline form",
            input:
            """
            /// <param name="source">The source text to parse. The text may contain comments, blank lines, quoted values, escaped characters, and export prefixes.</param>
            """,
            expected:
            """
            /// <param name="source">
            /// The source text to parse. The text may contain comments, blank lines, quoted values, escaped characters, and export
            /// prefixes.
            /// </param>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0011",
            name: "Whitespace collapses outside c tag but c tag content is preserved",
            input:
            """
            /// <summary>Parses   values   matching   <c>A   B   C</c>   exactly.</summary>
            """,
            expected:
            """
            /// <summary>
            /// Parses values matching <c>A   B   C</c> exactly.
            /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0012",
            name: "List structure is preserved with single-line rows",
            input:
            """
            /// <remarks><list type="bullet"><item><description>Blank lines are ignored.</description></item><item><description>Assignments are parsed as key-value pairs.</description></item></list></remarks>
            """,
            expected:
            """
            /// <remarks>
            /// <list type="bullet">
            /// <item>
            /// <description>Blank lines are ignored.</description>
            /// </item>
            /// <item>
            /// <description>Assignments are parsed as key-value pairs.</description>
            /// </item>
            /// </list>
            /// </remarks>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0013",
            name: "Inheritdoc self-closing tag remains single-line",
            input:
            """
            /// <inheritdoc />
            """,
            expected:
            """
            /// <inheritdoc />
            """,
            expectedChanged: false),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0014",
            name: "Indented XML documentation preserves base indentation",
            input:
            """
                /// <summary>Parses a DotEnv document.</summary>
            """,
            expected:
            """
                /// <summary>
                /// Parses a DotEnv document.
                /// </summary>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0015",
            name: "Malformed XML is left unchanged and classified",
            input:
            """
            /// <summary>
            /// Parses a <c>DotEnv document.
            /// </summary>
            """,
            expected:
            """
            /// <summary>
            /// Parses a <c>DotEnv document.
            /// </summary>
            """,
            expectedChanged: false,
            expectedFailure: CodeStyleKatFailureKind.MalformedXml),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0016",
            name: "Returns block with two inline see langword tags packs both atoms onto one line when they fit",
            input:
            """
            /// <returns>
            /// <see langword="true" /> when the value was encoded; otherwise,
            /// <see langword="false" /> when the destination buffer is too small.
            /// </returns>
            """,
            expected:
            """
            /// <returns>
            /// <see langword="true" /> when the value was encoded; otherwise, <see langword="false" /> when the destination buffer
            /// is too small.
            /// </returns>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0017",
            name: "Single-line returns with two inline see langword tags expands and preserves both atoms",
            input:
            """
            /// <returns><see langword="true" /> when the value was encoded; otherwise, <see langword="false" /> when the destination buffer is too small.</returns>
            """,
            expected:
            """
            /// <returns>
            /// <see langword="true" /> when the value was encoded; otherwise, <see langword="false" /> when the destination buffer
            /// is too small.
            /// </returns>
            """),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0018",
            name: "Two adjacent inline see tags separated only by whitespace are both preserved as atoms",
            input:
            """
            /// <summary>
            /// Either <see langword="true" /> or <see langword="false" />.
            /// </summary>
            """,
            expected:
            """
            /// <summary>
            /// Either <see langword="true" /> or <see langword="false" />.
            /// </summary>
            """,
            expectedChanged: false),

        CodeStyleKat.XmlFormat(
            id: "XMLFMT-0019",
            name: "Every <see> variant (cref / href / langword) stays inline inside another doc tag",
            input:
            """
            /// <summary>
            /// See <see cref="Sample" />, <see href="https://example.com" />, or pass <see langword="null" />.
            /// </summary>
            """,
            expected:
            """
            /// <summary>
            /// See <see cref="Sample" />, <see href="https://example.com" />, or pass <see langword="null" />.
            /// </summary>
            """,
            expectedChanged: false),

        // Analyzer KATs.
        CodeStyleKat.Analyze(
            id: "XMLANL-0001",
            name: "Single-line summary reports XML documentation diagnostic",
            source:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }
            }
            """,
            diagnostics:
            [
                new CodeStyleKatDiagnostic(CodeStyleKatConstants.XmlDocumentationDiagnosticId, Line: 5, Column: 5),
            ]),

        CodeStyleKat.Analyze(
            id: "XMLANL-0002",
            name: "Formatted summary reports no diagnostics",
            source:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses a DotEnv document.
                /// </summary>
                public static void Parse()
                {
                }
            }
            """,
            diagnostics: []),

        CodeStyleKat.Analyze(
            id: "XMLANL-0003",
            name: "CodeRush-broken c tag reports inline-code diagnostic",
            source:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Values matching <c>[A-Za-z0-
                /// 9_.,:/@+\-]</c> are unquoted.
                /// </summary>
                public static void Format()
                {
                }
            }
            """,
            diagnostics:
            [
                new CodeStyleKatDiagnostic(CodeStyleKatConstants.XmlDocInlineCodeDiagnosticId, Line: 5, Column: 5),
            ]),

        CodeStyleKat.Analyze(
            id: "XMLANL-0004",
            name: "Auto-generated file is skipped",
            source:
            """
            // <auto-generated />

            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }
            }
            """,
            diagnostics: []),

        // Code-fix KATs.
        CodeStyleKat.CodeFix(
            id: "XMLFIX-0001",
            name: "Formats single-line summary",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses a DotEnv document.
                /// </summary>
                public static void Parse()
                {
                }
            }
            """),

        CodeStyleKat.CodeFix(
            id: "XMLFIX-0002",
            name: "Repairs CodeRush-broken c tag by joining atomic inline code",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Values matching <c>[A-Za-z0-
                /// 9_.,:/@+\-]</c> are unquoted.
                /// </summary>
                public static string Format() => string.Empty;
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Values matching <c>[A-Za-z0-9_.,:/@+\-]</c> are unquoted.
                /// </summary>
                public static string Format() => string.Empty;
            }
            """),

        CodeStyleKat.CodeFix(
            id: "XMLFIX-0003",
            name: "Fix all in document formats multiple comments",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }

                /// <summary>Formats a DotEnv document.</summary>
                public static string Format() => string.Empty;
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses a DotEnv document.
                /// </summary>
                public static void Parse()
                {
                }

                /// <summary>
                /// Formats a DotEnv document.
                /// </summary>
                public static string Format() => string.Empty;
            }
            """,
            fixAllScope: CodeStyleKatFixAllScope.Document),

        CodeStyleKat.CodeFix(
            id: "XMLFIX-0004",
            name: "Malformed XML does not receive destructive fix",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses <c>broken docs.
                /// </summary>
                public static void Parse()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses <c>broken docs.
                /// </summary>
                public static void Parse()
                {
                }
            }
            """,
            shouldOfferFix: false),

        // Configuration KATs.
        CodeStyleKat.Configuration(
            id: "XMLCFG-0001",
            name: "Max line length from analyzer config controls wrapping",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document from source text using the configured DotEnv parsing policy.</summary>
                public static void Parse()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses a DotEnv document from source text using the configured
                /// DotEnv parsing policy.
                /// </summary>
                public static void Parse()
                {
                }
            }
            """,
            analyzerConfig:
            """
            root = true

            [*.cs]
            dotnet_analyzer_diagnostic.category-Documentation.severity = warning
            bodu_xmldoc_max_line_length = 72
            """),

        CodeStyleKat.Configuration(
            id: "XMLCFG-0002",
            name: "JSON policy can keep summary single-line when configured",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
                public static void Parse()
                {
                }
            }
            """,
            additionalFiles:
            [
                new CodeStyleKatAdditionalFile(
                    "bodu.xmldocstyle.json",
                    """
                    {
                      "profile": "Compact",
                      "maxLineLength": 120,
                      "inlineTags": [ "c", "see", "paramref", "typeparamref" ],
                      "neverSplitTagContent": [ "c", "see", "paramref", "typeparamref" ],
                      "tagPolicies": { "summary": { "layout": "singleLineWhenShort" } }
                    }
                    """),
            ]),

        // Safety KATs.
        CodeStyleKat.Safety(
            id: "XMLSAFE-0001",
            name: "Preprocessor directive between doc and member does not block fix",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>Parses a DotEnv document.</summary>
            #if DEBUG
                public static void Parse()
                {
                }
            #endif
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                /// <summary>
                /// Parses a DotEnv document.
                /// </summary>
            #if DEBUG
                public static void Parse()
                {
                }
            #endif
            }
            """),

        CodeStyleKat.Safety(
            id: "XMLSAFE-0002",
            name: "Regular comments are ignored",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                // <summary>Not XML documentation.</summary>
                public static void Parse()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class DotEnv
            {
                // <summary>Not XML documentation.</summary>
                public static void Parse()
                {
                }
            }
            """),

        CodeStyleKat.Safety(
            id: "XMLSAFE-0003",
            name: "Generated designer file is skipped by filename convention",
            inputSource:
            """
            namespace Bodu.Text.Formats;

            public static class GeneratedType
            {
                /// <summary>Generated member.</summary>
                public static void M()
                {
                }
            }
            """,
            expectedSource:
            """
            namespace Bodu.Text.Formats;

            public static class GeneratedType
            {
                /// <summary>Generated member.</summary>
                public static void M()
                {
                }
            }
            """,
            filePath: "GeneratedType.Designer.cs"),

        CodeStyleKat.Safety(
            id: "XMLSAFE-0004",
            name: "#pragma warning directive between XML doc and member does not block fix — long summary and returns lines",
            inputSource:
            """
            namespace Bodu.Extensions;

            public static partial class ArrayExtensions
            {
                /// <summary>
                /// Creates a shallow copy of the specified array of value types. Returns <see langword="null" /> if the source array is
                /// <see langword="null" />.
                /// </summary>
                /// <typeparam name="T">The value type of the elements in the array.</typeparam>
                /// <param name="array">The array to copy.</param>
                /// <returns>
                /// A new array containing the same elements as the source, or <see langword="null" /> if the source is <see langword="null" />.
                /// </returns>
            #pragma warning disable S1168
                public static T[] Copy<T>(this T[] array) where T : struct => default;
            #pragma warning restore S1168
            }
            """,
            expectedSource:
            """
            namespace Bodu.Extensions;

            public static partial class ArrayExtensions
            {
                /// <summary>
                /// Creates a shallow copy of the specified array of value types. Returns <see langword="null" /> if the source
                /// array is <see langword="null" />.
                /// </summary>
                /// <typeparam name="T">The value type of the elements in the array.</typeparam>
                /// <param name="array">The array to copy.</param>
                /// <returns>
                /// A new array containing the same elements as the source, or <see langword="null" /> if the source is
                /// <see langword="null" />.
                /// </returns>
            #pragma warning disable S1168
                public static T[] Copy<T>(this T[] array) where T : struct => default;
            #pragma warning restore S1168
            }
            """),

        CodeStyleKat.Safety(
            id: "XMLSAFE-0005",
            name: "#pragma warning directive between XML doc and method does not block fix — long para line in remarks",
            inputSource:
            """
            namespace Bodu.Extensions;

            public static partial class DateTimeExtensions
            {
                /// <summary>
                /// Returns the later of two specified nullable <see cref="System.DateTime" /> values.
                /// </summary>
                /// <param name="first">The first nullable <see cref="System.DateTime" /> value to compare.</param>
                /// <param name="second">The second nullable <see cref="System.DateTime" /> value to compare.</param>
                /// <returns>
                /// The later non-null <see cref="System.DateTime" /> value, or <see langword="null" /> if both values are <see langword="null" />.
                /// </returns>
                /// <remarks>
                /// <para>
                /// If both values are non-null, they are compared using the greater-than-or-equal-to (<c>&gt;=</c>) operator. If only one value is non-null, that value is returned. If both are <see langword="null" />, the result is <see langword="null" />.
                /// </para>
                /// </remarks>
            #pragma warning disable S3358
                public static System.DateTime? Max(System.DateTime? first, System.DateTime? second) => default;
            #pragma warning restore S3358
            }
            """,
            expectedSource:
            """
            namespace Bodu.Extensions;

            public static partial class DateTimeExtensions
            {
                /// <summary>
                /// Returns the later of two specified nullable <see cref="System.DateTime" /> values.
                /// </summary>
                /// <param name="first">The first nullable <see cref="System.DateTime" /> value to compare.</param>
                /// <param name="second">The second nullable <see cref="System.DateTime" /> value to compare.</param>
                /// <returns>
                /// The later non-null <see cref="System.DateTime" /> value, or <see langword="null" /> if both values are
                /// <see langword="null" />.
                /// </returns>
                /// <remarks>
                /// <para>
                /// If both values are non-null, they are compared using the greater-than-or-equal-to (<c>&gt;=</c>) operator. If
                /// only one value is non-null, that value is returned. If both are <see langword="null" />, the result is
                /// <see langword="null" />.
                /// </para>
                /// </remarks>
            #pragma warning disable S3358
                public static System.DateTime? Max(System.DateTime? first, System.DateTime? second) => default;
            #pragma warning restore S3358
            }
            """),

        CodeStyleKat.Safety(
            id: "XMLSAFE-0006",
            name: "#if/#else/#endif directive between XML doc and class does not block fix — long summary line",
            inputSource:
            """
            namespace Bodu.Collections.Generic;

            public partial class EvictingDictionary<TKey, TValue>
            {
                /// <summary>
                /// Represents a cache entry storing the value and metadata used for eviction policies in the <see cref="EvictingDictionary{TKey, TValue}"/>.
                /// </summary>
            #if !NET5_0_OR_GREATER
                private sealed class CacheItem { }
            #else
                private sealed record class CacheItem { }
            #endif
            }
            """,
            expectedSource:
            """
            namespace Bodu.Collections.Generic;

            public partial class EvictingDictionary<TKey, TValue>
            {
                /// <summary>
                /// Represents a cache entry storing the value and metadata used for eviction policies in the
                /// <see cref="EvictingDictionary{TKey, TValue}" />.
                /// </summary>
            #if !NET5_0_OR_GREATER
                private sealed class CacheItem { }
            #else
                private sealed record class CacheItem { }
            #endif
            }
            """),
    };

    /// <summary>
    /// Returns all KATs as MSTest-compatible dynamic data.
    /// </summary>
    public static IEnumerable<object[]> Data()
    {
        foreach (CodeStyleKat kat in All)
        {
            yield return new object[] { kat };
        }
    }

    /// <summary>
    /// Returns KATs in the specified area as MSTest-compatible dynamic data.
    /// </summary>
    public static IEnumerable<object[]> Data(CodeStyleKatArea area)
    {
        foreach (CodeStyleKat kat in All.Where(kat => kat.Area == area))
        {
            yield return new object[] { kat };
        }
    }

    /// <summary>
    /// Returns KATs for the specified operation as MSTest-compatible dynamic data.
    /// </summary>
    public static IEnumerable<object[]> Data(CodeStyleKatOperation operation)
    {
        foreach (CodeStyleKat kat in All.Where(kat => kat.Operation == operation))
        {
            yield return new object[] { kat };
        }
    }

    /// <summary>
    /// Returns a display name for MSTest dynamic data output.
    /// </summary>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data)
        => data.Length > 0 && data[0] is CodeStyleKat kat
            ? kat.ToString()
            : methodInfo.Name;
}
