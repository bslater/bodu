// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKnownAnswerData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#nullable enable

using System.Collections.ObjectModel;
using Bodu.Test.Kat;

namespace Bodu.Text.Configuration.Test.Infrastructure;

/// <summary>
/// Provides reusable known-answer-test data for the Bodu text configuration format.
/// </summary>
/// <remarks>
/// <para>
/// These vectors are intentionally implementation-facing. They are designed to validate the parser, writer, key mapper,
/// glob matcher, resolver, typed accessors, and Microsoft.Extensions.Configuration bridge.
/// </para>
/// <para>
/// The format is EditorConfig-inspired, not EditorConfig-compliant. Where behaviour differs from the EditorConfig
/// specification, the KAT explicitly identifies the expected Bodu behaviour.
/// </para>
/// </remarks>
public static class ConfigurationKnownAnswerData
{
    /// <summary>
    /// Gets the full KAT catalogue.
    /// </summary>
    public static IReadOnlyList<ConfigurationKat> All { get; } = CreateAll();

    /// <summary>
    /// Gets parser and lexer KATs.
    /// </summary>
    public static IEnumerable<object[]> ParserData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.Parse)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets key-mapping KATs.
    /// </summary>
    public static IEnumerable<object[]> KeyData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.KeyMapping)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets glob-pattern KATs.
    /// </summary>
    public static IEnumerable<object[]> PatternData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.Pattern)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets document resolution KATs.
    /// </summary>
    public static IEnumerable<object[]> ResolutionData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.Resolve)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets writer and round-trip KATs.
    /// </summary>
    public static IEnumerable<object[]> WriterData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.Write or ConfigurationKatKind.RoundTrip)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets typed accessor KATs.
    /// </summary>
    public static IEnumerable<object[]> TypedValueData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.TypedValue)
           .Select(kat => new object[] { kat });

    /// <summary>
    /// Gets Microsoft.Extensions.Configuration bridge KATs.
    /// </summary>
    public static IEnumerable<object[]> BridgeData =>
        All.Where(kat => kat.Kind is ConfigurationKatKind.ConfigurationBridge)
           .Select(kat => new object[] { kat });

    private static IReadOnlyList<ConfigurationKat> CreateAll()
    {
        var items = new List<ConfigurationKat>();

        AddParserKats(items);
        AddKeyKats(items);
        AddPatternKats(items);
        AddResolutionKats(items);
        AddWriterKats(items);
        AddTypedValueKats(items);
        AddBridgeKats(items);
        AddFailureKats(items);

        return new ReadOnlyCollection<ConfigurationKat>(items);
    }

    private static void AddParserKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0001",
            title: "Empty file parses to empty document",
            source: "",
            expected: ExpectedDocument.Empty()));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0002",
            title: "Whitespace-only file parses to empty document with blank-line trivia",
            source: "   \n\t\r\n",
            expected: ExpectedDocument.Empty(blankLineCount: 2)));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0003",
            title: "Hash and semicolon full-line comments are preserved",
            source:
                """
                # first
                ; second
                """,
            expected: ExpectedDocument.Empty(commentCount: 2)));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0004",
            title: "Preamble root property is parsed",
            source:
                """
                root = true
                """,
            expected: ExpectedDocument.Create(
                preamble: [ExpectedProperty.Of("root", "true")])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0005",
            title: "Preamble properties may contain dotted keys",
            source:
                """
                application.name = Bodu
                application.environment = Development
                """,
            expected: ExpectedDocument.Create(
                preamble:
                [
                    ExpectedProperty.Of("application.name", "Bodu", "application:name"),
                    ExpectedProperty.Of("application.environment", "Development", "application:environment"),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0006",
            title: "Simple section with two properties",
            source:
                """
                [*.cs]
                indent.style = space
                indent.size = 4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*.cs",
                    [
                        ExpectedProperty.Of("indent.style", "space", "indent:style"),
                        ExpectedProperty.Of("indent.size", "4", "indent:size"),
                    ]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0007",
            title: "Section pattern may contain inner brackets",
            source:
                """
                [[weird].txt]
                enabled = true
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("[weird].txt", [ExpectedProperty.Of("enabled", "true")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0008",
            title: "First equals separates key and value",
            source:
                """
                [*]
                connection.string = Server=localhost;Database=Bodu
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*",
                    [
                        ExpectedProperty.Of("connection.string", "Server=localhost;Database=Bodu", "connection:string"),
                    ]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0009",
            title: "Empty value after equals becomes empty string",
            source:
                """
                [*]
                optional.value =
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("optional.value", "", "optional:value")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0010",
            title: "Key and value are trimmed",
            source:
                """
                [*]
                   format.indent.size     =     4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("format.indent.size", "4", "format:indent:size")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0011",
            title: "Bodu mode parses whitespace-introduced hash inline comment",
            profile: "Bodu",
            source:
                """
                [*]
                format.indent.size = 4 # standard indent
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*",
                    [
                        ExpectedProperty.Of("format.indent.size", "4", "format:indent:size", inlineComment: " standard indent", inlineCommentPrefix: '#'),
                    ]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0012",
            title: "Bodu mode parses whitespace-introduced semicolon inline comment",
            profile: "Bodu",
            source:
                """
                [*]
                format.indent.style = space ; standard style
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*",
                    [
                        ExpectedProperty.Of("format.indent.style", "space", "format:indent:style", inlineComment: " standard style", inlineCommentPrefix: ';'),
                    ]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0013",
            title: "EditorConfig-compatible mode treats hash after value as literal value text",
            profile: "EditorConfigCompatible",
            source:
                """
                [*]
                url = https://example.com/a#fragment
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("url", "https://example.com/a#fragment")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0014",
            title: "Bodu whitespace inline-comment mode preserves URL fragment when hash is not whitespace-introduced",
            profile: "Bodu",
            source:
                """
                [*]
                url = https://example.com/a#fragment
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("url", "https://example.com/a#fragment")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0015",
            title: "Comments attach to following property",
            source:
                """
                [*]
                # controls indentation width
                format.indent.size = 4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*",
                    [
                        ExpectedProperty.Of("format.indent.size", "4", "format:indent:size", leadingComments: [" controls indentation width"]),
                    ]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0016",
            title: "Comments before section attach to section",
            source:
                """
                # applies to C# files
                [*.cs]
                enabled = true
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*.cs", [ExpectedProperty.Of("enabled", "true")], leadingComments: [" applies to C# files"]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0017",
            title: "Duplicate keys in LastWins mode keep effective last value",
            duplicateKeyMode: "LastWins",
            source:
                """
                [*]
                indent.size = 2
                indent.size = 4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("indent.size", "4", "indent:size")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0018",
            title: "Duplicate keys in FirstWins mode keep effective first value",
            duplicateKeyMode: "FirstWins",
            source:
                """
                [*]
                indent.size = 2
                indent.size = 4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("indent.size", "2", "indent:size")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0019",
            title: "Duplicate sections are preserved as ordered sections by default",
            source:
                """
                [*.cs]
                indent.size = 2

                [*.cs]
                indent.size = 4
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*.cs", [ExpectedProperty.Of("indent.size", "2", "indent:size")]),
                    ExpectedSection.Of("*.cs", [ExpectedProperty.Of("indent.size", "4", "indent:size")]),
                ])));

        items.Add(ConfigurationKat.ParsePass(
            id: "PARSE-0020",
            title: "Case-insensitive key storage recognises duplicate with different casing",
            duplicateKeyMode: "LastWins",
            source:
                """
                [*]
                Logging.Level.Default = Information
                logging.level.default = Warning
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("logging.level.default", "Warning", "logging:level:default")]),
                ])));
    }

    private static void AddKeyKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.KeyPass("KEY-0001", "Single segment key is unchanged", "name", "name", ["name"]));
        items.Add(ConfigurationKat.KeyPass("KEY-0002", "Dotted key maps to colon hierarchy", "application.name", "application:name", ["application", "name"]));
        items.Add(ConfigurationKat.KeyPass("KEY-0003", "Deep dotted key maps to deep hierarchy", "logging.level.default", "logging:level:default", ["logging", "level", "default"]));
        items.Add(ConfigurationKat.KeyPass("KEY-0004", "Underscore remains literal", "format.new_line.before_where_clause", "format:new_line:before_where_clause", ["format", "new_line", "before_where_clause"]));
        items.Add(ConfigurationKat.KeyPass("KEY-0005", "Hyphen remains literal", "tool.my-option.enabled", "tool:my-option:enabled", ["tool", "my-option", "enabled"]));
        items.Add(ConfigurationKat.KeyPass("KEY-0006", "Existing colon lookup normalizes to same logical key", "logging:level:default", "logging:level:default", ["logging", "level", "default"], mapping: "DotToColon"));
        items.Add(ConfigurationKat.KeyPass("KEY-0007", "Identity mapping preserves dotted key", "logging.level.default", "logging.level.default", ["logging", "level", "default"], mapping: "Identity"));
        items.Add(ConfigurationKat.KeyPass("KEY-0008", "Case-sensitive option preserves casing", "Logging.Level.Default", "Logging:Level:Default", ["Logging", "Level", "Default"], caseSensitive: true));

        items.Add(ConfigurationKat.KeyFail("KEY-1001", "Null key is rejected", null, "ArgumentNullException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1002", "Empty key is rejected", "", "ArgumentException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1003", "Whitespace-only key is rejected", "   ", "ArgumentException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1004", "Leading dot creates empty segment and is rejected", ".logging", "ArgumentException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1005", "Trailing dot creates empty segment and is rejected", "logging.", "ArgumentException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1006", "Consecutive dots create empty segment and are rejected", "logging..level", "ArgumentException"));
        items.Add(ConfigurationKat.KeyFail("KEY-1007", "Control character in key is rejected", "logging..level", "ArgumentException"));
    }

    private static void AddPatternKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.PatternPass("PAT-0001", "Star matches filename in same directory", "*.cs", "Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0002", "Star without slash matches at any depth", "*.cs", "src/Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0003", "Star does not cross slash inside same pattern segment", "src/*.cs", "src/Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0004", "Star does not cross slash into nested directory", "src/*.cs", "src/Generated/Program.cs", false));
        items.Add(ConfigurationKat.PatternPass("PAT-0005", "Double-star crosses directories", "src/**/*.cs", "src/Generated/Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0006", "Double-star can match zero directories", "src/**/*.cs", "src/Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0007", "Question matches one non-slash character", "file?.txt", "file1.txt", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0008", "Question does not match zero characters", "file?.txt", "file.txt", false));
        items.Add(ConfigurationKat.PatternPass("PAT-0009", "Brace alternation matches first choice", "*.{json,yaml,yml}", "appsettings.json", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0010", "Brace alternation matches later choice", "*.{json,yaml,yml}", "workflow.yaml", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0011", "Brace alternation rejects non-choice", "*.{json,yaml,yml}", "Program.cs", false));
        items.Add(ConfigurationKat.PatternPass("PAT-0012", "Escaped star is literal", @"file\*.txt", "file*.txt", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0013", "Escaped star does not behave as wildcard", @"file\*.txt", "file1.txt", false));
        items.Add(ConfigurationKat.PatternPass("PAT-0014", "Character class matches included character", "file[123].txt", "file2.txt", true, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0015", "Character class rejects excluded character", "file[123].txt", "file4.txt", false, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0016", "Negated character class rejects listed character", "file[!123].txt", "file2.txt", false, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0017", "Negated character class accepts unlisted character", "file[!123].txt", "file4.txt", true, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0018", "Numeric range matches interior value", "file{1..10}.txt", "file7.txt", true, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0019", "Numeric range rejects outside value", "file{1..10}.txt", "file11.txt", false, stage: "P1"));
        items.Add(ConfigurationKat.PatternPass("PAT-0020", "Backslash is normalized to forward slash before matching", "*.cs", @"src\Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0021", "Forward slash is canonical separator", "src/*.cs", "src/Program.cs", true));
        items.Add(ConfigurationKat.PatternPass("PAT-0022", "Pattern with slash is anchored to path root", "src/*.cs", "other/src/Program.cs", false));
        items.Add(ConfigurationKat.PatternPass("PAT-0023", "Pattern without slash matches basename at any depth", "*.cs", "other/src/Program.cs", true));

        items.Add(ConfigurationKat.PatternFail("PAT-1001", "Unbalanced brace is rejected", "*.{cs,json", "ConfigurationParseException"));
        items.Add(ConfigurationKat.PatternFail("PAT-1002", "Unbalanced character class is rejected", "file[abc.txt", "ConfigurationParseException", stage: "P1"));
    }

    private static void AddResolutionKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0001",
            title: "Preamble participates in Bodu resolution",
            targetPath: "src/Program.cs",
            source:
                """
                application.name = Bodu

                [*.cs]
                format.indent.size = 4
                """,
            expectedValues:
            [
                ExpectedValue.Of("application:name", "Bodu"),
                ExpectedValue.Of("format:indent:size", "4"),
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0002",
            title: "Preamble does not participate in EditorConfig-compatible resolution except root",
            profile: "EditorConfigCompatible",
            targetPath: "src/Program.cs",
            source:
                """
                application.name = Bodu
                root = true

                [*.cs]
                format.indent.size = 4
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "4"),
            ],
            unexpectedKeys:
            [
                "application:name",
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0003",
            title: "Later matching section overrides earlier matching section",
            targetPath: "src/Program.cs",
            source:
                """
                [*]
                format.indent.size = 2

                [*.cs]
                format.indent.size = 4
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "4"),
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0004",
            title: "Non-matching section does not apply",
            targetPath: "src/Program.cs",
            source:
                """
                [*.json]
                format.indent.size = 2

                [*.cs]
                format.indent.size = 4
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "4"),
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0005",
            title: "Brace pattern section applies to csproj file",
            targetPath: "src/Bodu.Text.Configuration/Bodu.Text.Configuration.csproj",
            source:
                """
                [src/**/*.{cs,csproj}]
                logging.level.default = Warning
                """,
            expectedValues:
            [
                ExpectedValue.Of("logging:level:default", "Warning"),
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0006",
            title: "Unset removes earlier effective value in EditorConfig-compatible mode",
            profile: "EditorConfigCompatible",
            targetPath: "generated/Program.cs",
            source:
                """
                [*.cs]
                format.indent.size = 4

                [generated/**]
                format.indent.size = unset
                """,
            expectedValues: [],
            unexpectedKeys:
            [
                "format:indent:size",
            ],
            options: "UnsetRemovesEffectiveValue"));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0007",
            title: "Unset can be treated as literal in Bodu mode",
            profile: "Bodu",
            targetPath: "generated/Program.cs",
            source:
                """
                [*.cs]
                format.indent.size = 4

                [generated/**]
                format.indent.size = unset
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "unset"),
            ],
            options: "UnsetTreatAsLiteral"));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0008",
            title: "Resolved view supports lookup by dotted key",
            targetPath: "src/Program.cs",
            source:
                """
                [*.cs]
                logging.level.default = Information
                """,
            expectedValues:
            [
                ExpectedValue.Of("logging.level.default", "Information"),
                ExpectedValue.Of("logging:level:default", "Information"),
            ]));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0009",
            title: "Resolved view is snapshot and does not observe later document mutation",
            targetPath: "src/Program.cs",
            source:
                """
                [*.cs]
                value = before
                """,
            expectedValues:
            [
                ExpectedValue.Of("value", "before"),
            ],
            mutation: ExpectedMutation.SetAfterResolve(section: "*.cs", key: "value", value: "after")));

        items.Add(ConfigurationKat.ResolvePass(
            id: "RES-0010",
            title: "Case-insensitive effective keys collapse to last value",
            targetPath: "src/Program.cs",
            source:
                """
                [*.cs]
                Logging.Level.Default = Information
                logging.level.default = Warning
                """,
            expectedValues:
            [
                ExpectedValue.Of("logging:level:default", "Warning"),
            ]));

        items.Add(ConfigurationKat.ResolveFail(
            id: "RES-1001",
            title: "Resolve with anchored pattern and no path root throws in strict path-root mode",
            targetPath: null,
            source:
                """
                [src/*.cs]
                enabled = true
                """,
            expectedException: "InvalidOperationException",
            options: "RequirePathRoot"));
    }

    private static void AddWriterKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.WritePass(
            id: "WRITE-0001",
            title: "Minimal document writes preamble and section",
            source:
                """
                root = true

                [*.cs]
                indent.size = 4
                """,
            expectedText:
                """
                root = true

                [*.cs]
                indent.size = 4
                """));

        items.Add(ConfigurationKat.WritePass(
            id: "WRITE-0003",
            title: "Bodu writer preserves inline comments",
            profile: "Bodu",
            source:
                """
                [*]
                indent.size = 4 # standard
                """,
            expectedText:
                """
                [*]
                indent.size = 4 # standard
                """));

        items.Add(ConfigurationKat.WritePass(
            id: "WRITE-0004",
            title: "EditorConfig-compatible writer drops inline comments",
            profile: "EditorConfigCompatible",
            options: "WriteInlineCommentsFalse",
            source:
                """
                [*]
                indent.size = 4 # standard
                """,
            expectedText:
                """
                [*]
                indent.size = 4
                """));

        items.Add(ConfigurationKat.WritePass(
            id: "WRITE-0005",
            title: "Writer preserves full-line comments",
            source:
                """
                # repo setting
                root = true

                # C# files
                [*.cs]
                # indentation
                indent.size = 4
                """,
            expectedText:
                """
                # repo setting
                root = true

                # C# files
                [*.cs]
                # indentation
                indent.size = 4
                """));

        items.Add(ConfigurationKat.RoundTripPass(
            id: "ROUND-0001",
            title: "Round-trip preserves ordering, comments, and values",
            source:
                """
                # bodu.config
                root = true

                [*.cs]
                indent.style = space
                indent.size = 4

                [src/**/*.{cs,csproj}]
                indent.size = 2
                logging.level.default = Warning
                """));

        items.Add(ConfigurationKat.RoundTripPass(
            id: "ROUND-0002",
            title: "Round-trip preserves weird section name with internal brackets",
            source:
                """
                [[generated].cs]
                enabled = false
                """));

        items.Add(ConfigurationKat.RoundTripPass(
            id: "ROUND-0003",
            title: "Round-trip preserves empty values",
            source:
                """
                [*]
                empty =
                """));
    }

    private static void AddTypedValueKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.TypedPass("TYPE-0001", "Boolean true parses", "feature.enabled", "true", "Boolean", true));
        items.Add(ConfigurationKat.TypedPass("TYPE-0002", "Boolean false parses", "feature.enabled", "false", "Boolean", false));
        items.Add(ConfigurationKat.TypedPass("TYPE-0003", "Boolean parsing is case-insensitive", "feature.enabled", "TrUe", "Boolean", true));
        items.Add(ConfigurationKat.TypedPass("TYPE-0004", "Int32 positive parses", "indent.size", "4", "Int32", 4));
        items.Add(ConfigurationKat.TypedPass("TYPE-0005", "Int32 negative parses", "offset", "-1", "Int32", -1));
        items.Add(ConfigurationKat.TypedPass("TYPE-0006", "Int64 parses large value", "max.bytes", "9223372036854775807", "Int64", 9223372036854775807L));
        items.Add(ConfigurationKat.TypedPass("TYPE-0007", "Enum parses named value", "severity", "Warning", "Enum", "Warning"));
        items.Add(ConfigurationKat.TypedPass("TYPE-0008", "Fallback returns only when key is missing", "missing.value", null, "Int32WithFallback", 42));

        items.Add(ConfigurationKat.TypedFail("TYPE-1001", "Malformed Int32 throws even with fallback", "indent.size", "four", "Int32WithFallback", "FormatException"));
        items.Add(ConfigurationKat.TypedFail("TYPE-1002", "Overflowing Int32 throws", "indent.size", "2147483648", "Int32", "FormatException"));
        items.Add(ConfigurationKat.TypedFail("TYPE-1003", "Malformed Boolean throws", "feature.enabled", "yes please", "Boolean", "FormatException"));
        items.Add(ConfigurationKat.TypedFail("TYPE-1004", "Malformed Enum throws", "severity", "Maybe", "Enum", "FormatException"));
        items.Add(ConfigurationKat.TypedFail("TYPE-1005", "Required missing key throws", "missing.value", null, "Int32", "KeyNotFoundException"));
    }

    private static void AddBridgeKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-0001",
            title: "Bridge emits colon-delimited key for dotted file key",
            targetPath: "src/Program.cs",
            source:
                """
                [*.cs]
                logging.level.default = Information
                """,
            expectedValues:
            [
                ExpectedValue.Of("logging:level:default", "Information"),
            ]));

        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-0002",
            title: "Bridge resolves target path before populating provider data",
            targetPath: "appsettings.json",
            source:
                """
                [*]
                format.indent.size = 4

                [*.json]
                format.indent.size = 2
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "2"),
            ]));

        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-0003",
            title: "Bridge omits non-matching section values",
            targetPath: "Program.cs",
            source:
                """
                [*.json]
                format.indent.size = 2

                [*.cs]
                format.indent.size = 4
                """,
            expectedValues:
            [
                ExpectedValue.Of("format:indent:size", "4"),
            ]));

        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-0004",
            title: "Bridge supports hierarchical GetSection shape",
            targetPath: "Program.cs",
            source:
                """
                [*.cs]
                logging.level.default = Information
                logging.level.microsoft = Warning
                """,
            expectedValues:
            [
                ExpectedValue.Of("logging:level:default", "Information"),
                ExpectedValue.Of("logging:level:microsoft", "Warning"),
            ]));

        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-0005",
            title: "Bridge can load Bodu preamble as global values",
            targetPath: "Program.cs",
            source:
                """
                application.name = Bodu

                [*.cs]
                enabled = true
                """,
            expectedValues:
            [
                ExpectedValue.Of("application:name", "Bodu"),
                ExpectedValue.Of("enabled", "true"),
            ]));

        items.Add(ConfigurationKat.BridgeFail(
            id: "BRIDGE-1001",
            title: "Non-optional missing file throws",
            expectedException: "FileNotFoundException",
            options: "OptionalFalseMissingFile"));

        items.Add(ConfigurationKat.BridgePass(
            id: "BRIDGE-1002",
            title: "Optional missing file does not throw and emits no values",
            source: null,
            targetPath: "Program.cs",
            expectedValues: [],
            options: "OptionalTrueMissingFile"));
    }

    private static void AddFailureKats(List<ConfigurationKat> items)
    {
        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0001",
            title: "Missing equals is rejected",
            source:
                """
                [*]
                indent.size 4
                """,
            expectedException: "ConfigurationParseException",
            expectedDiagnosticCode: "MissingEquals"));

        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0002",
            title: "Empty key is rejected",
            source:
                """
                [*]
                   = value
                """,
            expectedException: "ConfigurationParseException",
            expectedDiagnosticCode: "EmptyKey"));

        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0003",
            title: "Unterminated section header is rejected",
            source:
                """
                [*.cs
                indent.size = 4
                """,
            expectedException: "ConfigurationParseException",
            expectedDiagnosticCode: "UnterminatedSectionHeader"));

        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0005",
            title: "Duplicate key rejects in Disallowed mode",
            duplicateKeyMode: "Disallowed",
            source:
                """
                [*]
                indent.size = 2
                indent.size = 4
                """,
            expectedException: "ConfigurationParseException",
            expectedDiagnosticCode: "DuplicateKey"));

        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0006",
            title: "Key-only property is rejected by default",
            source:
                """
                [*]
                keyOnly
                """,
            expectedException: "ConfigurationParseException",
            expectedDiagnosticCode: "MissingEquals"));

        items.Add(ConfigurationKat.ParsePass(
            id: "FAIL-0007",
            title: "Key-only property is accepted in relaxed option mode",
            options: "AllowKeyOnlyProperties",
            source:
                """
                [*]
                keyOnly
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*", [ExpectedProperty.Of("keyOnly", "")]),
                ])));

        items.Add(ConfigurationKat.ParseFail(
            id: "FAIL-0010",
            title: "Null source passed to Parse throws",
            source: null,
            expectedException: "ArgumentNullException"));

        items.Add(ConfigurationKat.ParsePass(
            id: "FAIL-0013",
            title: "Ignore diagnostic mode suppresses recoverable diagnostic list",
            diagnosticMode: "Ignore",
            source:
                """
                [*]
                valid = true
                bad line
                another.valid = true
                """,
            expected: ExpectedDocument.Create(
                sections:
                [
                    ExpectedSection.Of("*",
                    [
                        ExpectedProperty.Of("valid", "true"),
                        ExpectedProperty.Of("another.valid", "true", "another:valid"),
                    ]),
                ]),
            expectedDiagnosticCount: 0));
    }
}

/// <summary>
/// Describes the subsystem validated by a known-answer test.
/// </summary>
public enum ConfigurationKatKind
{
    /// <summary>
    /// Parser or lexer behaviour.
    /// </summary>
    Parse,

    /// <summary>
    /// Writer behaviour.
    /// </summary>
    Write,

    /// <summary>
    /// Parse-write-parse round-trip behaviour.
    /// </summary>
    RoundTrip,

    /// <summary>
    /// Key mapping behaviour.
    /// </summary>
    KeyMapping,

    /// <summary>
    /// Glob/pattern matching behaviour.
    /// </summary>
    Pattern,

    /// <summary>
    /// Document resolution behaviour.
    /// </summary>
    Resolve,

    /// <summary>
    /// Typed value getter behaviour.
    /// </summary>
    TypedValue,

    /// <summary>
    /// Microsoft.Extensions.Configuration bridge behaviour.
    /// </summary>
    ConfigurationBridge,
}

/// <summary>
/// Indicates whether a known-answer test expects success or failure.
/// </summary>
public enum ConfigurationKatOutcome
{
    /// <summary>
    /// The scenario should succeed.
    /// </summary>
    Pass,

    /// <summary>
    /// The scenario should fail.
    /// </summary>
    Fail,
}

/// <summary>
/// Represents a known-answer test case for Bodu text configuration.
/// </summary>
public sealed record ConfigurationKat : IKat
{
    private ConfigurationKat(
        string id,
        string title,
        ConfigurationKatKind kind,
        ConfigurationKatOutcome outcome)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Kind = kind;
        Outcome = outcome;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="Title" /> so the row participates in <see cref="KatDisplayName" />-driven display formatting.
    /// </remarks>
    string IKat.Name => Title;

    /// <summary>
    /// Gets the stable test identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the subsystem kind.
    /// </summary>
    public ConfigurationKatKind Kind { get; }

    /// <summary>
    /// Gets the expected pass/fail outcome.
    /// </summary>
    public ConfigurationKatOutcome Outcome { get; }

    /// <summary>
    /// Gets the milestone or stage, such as M1, M2, M4, M5, M6, P0, or P1.
    /// </summary>
    public string Stage { get; init; } = "M1";

    /// <summary>
    /// Gets the profile name to use for the test.
    /// </summary>
    public string Profile { get; init; } = "Bodu";

    /// <summary>
    /// Gets optional option-set hints used by the test harness.
    /// </summary>
    public string? Options { get; init; }

    /// <summary>
    /// Gets the duplicate-key mode hint, when relevant.
    /// </summary>
    public string? DuplicateKeyMode { get; init; }

    /// <summary>
    /// Gets the diagnostic mode hint, when relevant.
    /// </summary>
    public string? DiagnosticMode { get; init; }

    /// <summary>
    /// Gets the source text to parse.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Gets the expected writer output text.
    /// </summary>
    public string? ExpectedText { get; init; }

    /// <summary>
    /// Gets the target path used for pattern matching or resolution.
    /// </summary>
    public string? TargetPath { get; init; }

    /// <summary>
    /// Gets the path root used for anchored pattern matching.
    /// </summary>
    public string? PathRoot { get; init; }

    /// <summary>
    /// Gets the raw key for key-mapping or typed-value tests.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the raw value for typed-value tests.
    /// </summary>
    public string? RawValue { get; init; }

    /// <summary>
    /// Gets the typed getter to exercise.
    /// </summary>
    public string? TypedAccessor { get; init; }

    /// <summary>
    /// Gets the expected typed value.
    /// </summary>
    public object? ExpectedTypedValue { get; init; }

    /// <summary>
    /// Gets the raw pattern for pattern tests.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets the expected pattern match result.
    /// </summary>
    public bool? ExpectedMatch { get; init; }

    /// <summary>
    /// Gets the expected document model.
    /// </summary>
    public ExpectedDocument? ExpectedDocument { get; init; }

    /// <summary>
    /// Gets expected resolved or bridge values.
    /// </summary>
    public IReadOnlyList<ExpectedValue> ExpectedValues { get; init; } = [];

    /// <summary>
    /// Gets keys expected to be absent from the resolved view.
    /// </summary>
    public IReadOnlyList<string> UnexpectedKeys { get; init; } = [];

    /// <summary>
    /// Gets the expected mapped configuration key for key tests.
    /// </summary>
    public string? ExpectedConfigurationKey { get; init; }

    /// <summary>
    /// Gets the expected key segments for key tests.
    /// </summary>
    public IReadOnlyList<string> ExpectedSegments { get; init; } = [];

    /// <summary>
    /// Gets the expected exception type name.
    /// </summary>
    public string? ExpectedException { get; init; }

    /// <summary>
    /// Gets the expected diagnostic code.
    /// </summary>
    public string? ExpectedDiagnosticCode { get; init; }

    /// <summary>
    /// Gets the expected diagnostic count when a precise count is required.
    /// </summary>
    public int? ExpectedDiagnosticCount { get; init; }

    /// <summary>
    /// Gets a mutation that should be applied after resolution to validate snapshot behaviour.
    /// </summary>
    public ExpectedMutation? Mutation { get; init; }

    /// <summary>
    /// Gets optional notes for implementers.
    /// </summary>
    public string? Notes { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Id}: {Title}";

    public static ConfigurationKat ParsePass(
        string id,
        string title,
        string source,
        ExpectedDocument expected,
        string profile = "Bodu",
        string? options = null,
        string? duplicateKeyMode = null,
        string? diagnosticMode = null,
        int? expectedDiagnosticCount = null) =>
        new(id, title, ConfigurationKatKind.Parse, ConfigurationKatOutcome.Pass)
        {
            Source = source,
            ExpectedDocument = expected,
            Profile = profile,
            Options = options,
            DuplicateKeyMode = duplicateKeyMode,
            DiagnosticMode = diagnosticMode,
            ExpectedDiagnosticCount = expectedDiagnosticCount,
            Stage = "M1",
        };

    public static ConfigurationKat ParseFail(
        string id,
        string title,
        string? source,
        string? expectedException,
        string? expectedDiagnosticCode = null,
        string profile = "Bodu",
        string? options = null,
        string? duplicateKeyMode = null,
        string? diagnosticMode = null) =>
        new(id, title, ConfigurationKatKind.Parse, ConfigurationKatOutcome.Fail)
        {
            Source = source,
            ExpectedException = expectedException,
            ExpectedDiagnosticCode = expectedDiagnosticCode,
            Profile = profile,
            Options = options,
            DuplicateKeyMode = duplicateKeyMode,
            DiagnosticMode = diagnosticMode,
            Stage = "M1",
        };

    public static ConfigurationKat KeyPass(
        string id,
        string title,
        string rawKey,
        string expectedConfigurationKey,
        IReadOnlyList<string> segments,
        string mapping = "DotToColon",
        bool caseSensitive = false) =>
        new(id, title, ConfigurationKatKind.KeyMapping, ConfigurationKatOutcome.Pass)
        {
            Key = rawKey,
            ExpectedConfigurationKey = expectedConfigurationKey,
            ExpectedSegments = segments,
            Options = $"{mapping};CaseSensitive={caseSensitive}",
            Stage = "M1",
        };

    public static ConfigurationKat KeyFail(
        string id,
        string title,
        string? rawKey,
        string expectedException,
        string mapping = "DotToColon") =>
        new(id, title, ConfigurationKatKind.KeyMapping, ConfigurationKatOutcome.Fail)
        {
            Key = rawKey,
            ExpectedException = expectedException,
            Options = mapping,
            Stage = "M1",
        };

    public static ConfigurationKat PatternPass(
        string id,
        string title,
        string pattern,
        string targetPath,
        bool expectedMatch,
        string stage = "P0",
        string? options = null) =>
        new(id, title, ConfigurationKatKind.Pattern, ConfigurationKatOutcome.Pass)
        {
            Pattern = pattern,
            TargetPath = targetPath,
            ExpectedMatch = expectedMatch,
            Stage = stage,
            Options = options,
        };

    public static ConfigurationKat PatternFail(
        string id,
        string title,
        string pattern,
        string expectedException,
        string stage = "P0",
        string? options = null) =>
        new(id, title, ConfigurationKatKind.Pattern, ConfigurationKatOutcome.Fail)
        {
            Pattern = pattern,
            ExpectedException = expectedException,
            Stage = stage,
            Options = options,
        };

    public static ConfigurationKat ResolvePass(
        string id,
        string title,
        string targetPath,
        string source,
        IReadOnlyList<ExpectedValue> expectedValues,
        IReadOnlyList<string>? unexpectedKeys = null,
        string profile = "Bodu",
        string? options = null,
        ExpectedMutation? mutation = null) =>
        new(id, title, ConfigurationKatKind.Resolve, ConfigurationKatOutcome.Pass)
        {
            TargetPath = targetPath,
            Source = source,
            ExpectedValues = expectedValues,
            UnexpectedKeys = unexpectedKeys ?? [],
            Profile = profile,
            Options = options,
            Mutation = mutation,
            Stage = "M5",
        };

    public static ConfigurationKat ResolveFail(
        string id,
        string title,
        string? targetPath,
        string source,
        string expectedException,
        string? options = null) =>
        new(id, title, ConfigurationKatKind.Resolve, ConfigurationKatOutcome.Fail)
        {
            TargetPath = targetPath,
            Source = source,
            ExpectedException = expectedException,
            Options = options,
            Stage = "M5",
        };

    public static ConfigurationKat WritePass(
        string id,
        string title,
        string source,
        string expectedText,
        string profile = "Bodu",
        string? options = null) =>
        new(id, title, ConfigurationKatKind.Write, ConfigurationKatOutcome.Pass)
        {
            Source = source,
            ExpectedText = expectedText,
            Profile = profile,
            Options = options,
            Stage = "M2",
        };

    public static ConfigurationKat RoundTripPass(
        string id,
        string title,
        string source,
        string profile = "Bodu",
        string? options = null) =>
        new(id, title, ConfigurationKatKind.RoundTrip, ConfigurationKatOutcome.Pass)
        {
            Source = source,
            ExpectedText = source,
            Profile = profile,
            Options = options,
            Stage = "M2",
        };

    public static ConfigurationKat TypedPass(
        string id,
        string title,
        string key,
        string? rawValue,
        string accessor,
        object? expectedValue) =>
        new(id, title, ConfigurationKatKind.TypedValue, ConfigurationKatOutcome.Pass)
        {
            Key = key,
            RawValue = rawValue,
            TypedAccessor = accessor,
            ExpectedTypedValue = expectedValue,
            Stage = "M5",
        };

    public static ConfigurationKat TypedFail(
        string id,
        string title,
        string key,
        string? rawValue,
        string accessor,
        string expectedException) =>
        new(id, title, ConfigurationKatKind.TypedValue, ConfigurationKatOutcome.Fail)
        {
            Key = key,
            RawValue = rawValue,
            TypedAccessor = accessor,
            ExpectedException = expectedException,
            Stage = "M5",
        };

    public static ConfigurationKat BridgePass(
        string id,
        string title,
        string? source,
        string? targetPath,
        IReadOnlyList<ExpectedValue> expectedValues,
        string? options = null) =>
        new(id, title, ConfigurationKatKind.ConfigurationBridge, ConfigurationKatOutcome.Pass)
        {
            Source = source,
            TargetPath = targetPath,
            ExpectedValues = expectedValues,
            Options = options,
            Stage = "M6",
        };

    public static ConfigurationKat BridgeFail(
        string id,
        string title,
        string expectedException,
        string? source = null,
        string? targetPath = null,
        string? options = null) =>
        new(id, title, ConfigurationKatKind.ConfigurationBridge, ConfigurationKatOutcome.Fail)
        {
            Source = source,
            TargetPath = targetPath,
            ExpectedException = expectedException,
            Options = options,
            Stage = "M6",
        };
}

/// <summary>
/// Represents the expected document model after parsing.
/// </summary>
public sealed record ExpectedDocument(
    IReadOnlyList<ExpectedProperty> Preamble,
    IReadOnlyList<ExpectedSection> Sections,
    int LeadingCommentCount = 0,
    int BlankLineCount = 0,
    int DiagnosticCount = 0)
{
    /// <summary>
    /// Creates an empty document expectation.
    /// </summary>
    public static ExpectedDocument Empty(int commentCount = 0, int blankLineCount = 0) =>
        new([], [], commentCount, blankLineCount);

    /// <summary>
    /// Creates a document expectation.
    /// </summary>
    public static ExpectedDocument Create(
        IReadOnlyList<ExpectedProperty>? preamble = null,
        IReadOnlyList<ExpectedSection>? sections = null,
        int commentCount = 0,
        int blankLineCount = 0,
        int diagnosticCount = 0) =>
        new(preamble ?? [], sections ?? [], commentCount, blankLineCount, diagnosticCount);
}

/// <summary>
/// Represents an expected section.
/// </summary>
public sealed record ExpectedSection(
    string Pattern,
    IReadOnlyList<ExpectedProperty> Properties,
    IReadOnlyList<string> LeadingComments)
{
    /// <summary>
    /// Creates an expected section.
    /// </summary>
    public static ExpectedSection Of(
        string pattern,
        IReadOnlyList<ExpectedProperty> properties,
        IReadOnlyList<string>? leadingComments = null) =>
        new(pattern, properties, leadingComments ?? []);
}

/// <summary>
/// Represents an expected property.
/// </summary>
public sealed record ExpectedProperty(
    string Key,
    string Value,
    string Path,
    IReadOnlyList<string> LeadingComments,
    string? InlineComment,
    char? InlineCommentPrefix)
{
    /// <summary>
    /// Creates an expected property.
    /// </summary>
    public static ExpectedProperty Of(
        string key,
        string value,
        string? path = null,
        IReadOnlyList<string>? leadingComments = null,
        string? inlineComment = null,
        char? inlineCommentPrefix = null) =>
        new(key, value, path ?? key.Replace('.', ':'), leadingComments ?? [], inlineComment, inlineCommentPrefix);
}

/// <summary>
/// Represents an expected resolved value.
/// </summary>
public sealed record ExpectedValue(string Key, string? Value)
{
    /// <summary>
    /// Creates an expected resolved value.
    /// </summary>
    public static ExpectedValue Of(string key, string? value) => new(key, value);
}

/// <summary>
/// Describes a post-resolution mutation used to verify snapshot semantics.
/// </summary>
public sealed record ExpectedMutation(string Section, string Key, string Value)
{
    /// <summary>
    /// Creates a mutation that sets a value after resolution.
    /// </summary>
    public static ExpectedMutation SetAfterResolve(string section, string key, string value) =>
        new(section, key, value);
}
