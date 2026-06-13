// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="TomlDocument.Parse(string)" /> and its overloads accept exactly the TOML grammar —
/// rejecting malformed input and over-deep nesting with <see cref="TomlFormatException" /> — and honour the
/// specification version and depth limit selected through <see cref="TomlDocumentOptions" />.
/// </summary>
public partial class TomlDocumentTests
{
    /// <summary>
    /// Yields a broad catalogue of malformed TOML documents that <see cref="TomlDocument.Parse(string)" /> must reject
    /// with <see cref="TomlFormatException" />.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per malformed-input scenario.</returns>
    /// <remarks>
    /// The rows sweep the grammar dimensions — key/value structure, strings and escapes, numbers, date-times, tables,
    /// arrays, and inline tables — mirroring the reader-level malformed catalogue at the document layer.
    /// </remarks>
    public static IEnumerable<object[]> MalformedDocuments()
    {
        static object[] Row(string name, string toml) => [new InvalidKat<string>(name, toml, typeof(TomlFormatException))];

        // Key/value structure.
        yield return Row("key without equals", "a 1\n");
        yield return Row("key without value", "a =\n");
        yield return Row("value without key", "= 1\n");
        yield return Row("duplicate key", "a = 1\na = 2\n");
        yield return Row("duplicate dotted key", "a.b = 1\na.b = 2\n");
        yield return Row("missing newline between pairs", "a = 1 b = 2\n");

        // Strings and escapes.
        yield return Row("unterminated basic string", "a = \"abc\n");
        yield return Row("unterminated literal string", "a = 'abc\n");
        yield return Row("invalid escape", "a = \"\\q\"\n");
        yield return Row("short unicode escape truncated", "a = \"\\u12\"\n");

        // Numbers.
        yield return Row("integer leading zero", "a = 01\n");
        yield return Row("integer overflow", "a = 9223372036854775808\n");
        yield return Row("float missing fraction digits", "a = 5.\n");
        yield return Row("float bad exponent", "a = 1e\n");

        // Date-times.
        yield return Row("month out of range", "a = 2020-13-01\n");
        yield return Row("leap second", "a = 23:59:60\n");

        // Booleans.
        yield return Row("capitalized true", "a = True\n");

        // Tables.
        yield return Row("unterminated table header", "[a\n");
        yield return Row("duplicate table", "[a]\n[a]\n");
        yield return Row("table over value", "a = 1\n[a]\n");
        yield return Row("array-of-tables over table", "[a]\n[[a]]\n");

        // Arrays and inline tables.
        yield return Row("unterminated array", "a = [1, 2\n");
        yield return Row("array missing comma", "a = [1 2]\n");
        yield return Row("unterminated inline table", "a = {x = 1\n");
        yield return Row("inline table duplicate key", "a = {x = 1, x = 2}\n");
    }

    /// <summary>
    /// Verifies that each malformed TOML document in the catalogue is rejected by
    /// <see cref="TomlDocument.Parse(string)" /> with <see cref="TomlFormatException" /> whose source location is
    /// populated.
    /// </summary>
    /// <param name="kat">The malformed-input scenario under test.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedDocuments), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenInputMalformed_ShouldThrowTomlFormatException(InvalidKat<string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var document = TomlDocument.Parse(kat.Input);
        });

        Assert.IsNotNull(ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that grammar edge-case documents parse and the named root member reports the expected value kind,
    /// covering every member of <see cref="TomlValueKind" /> plus empty containers.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The valid TOML document.</param>
    /// <param name="key">The root member whose kind is asserted.</param>
    /// <param name="expectedKind">The expected value kind of the member.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("basic string", "a = \"x\"\n", "a", TomlValueKind.String)]
    [DataRow("literal string", "a = 'x'\n", "a", TomlValueKind.String)]
    [DataRow("multiline string", "a = \"\"\"x\"\"\"\n", "a", TomlValueKind.String)]
    [DataRow("integer", "a = 42\n", "a", TomlValueKind.Integer)]
    [DataRow("hex integer", "a = 0xFF\n", "a", TomlValueKind.Integer)]
    [DataRow("float", "a = 1.5\n", "a", TomlValueKind.Float)]
    [DataRow("float inf", "a = inf\n", "a", TomlValueKind.Float)]
    [DataRow("float nan", "a = nan\n", "a", TomlValueKind.Float)]
    [DataRow("boolean", "a = true\n", "a", TomlValueKind.Boolean)]
    [DataRow("offset date-time", "a = 1979-05-27T07:32:00Z\n", "a", TomlValueKind.OffsetDateTime)]
    [DataRow("local date-time", "a = 1979-05-27T07:32:00\n", "a", TomlValueKind.LocalDateTime)]
    [DataRow("local date", "a = 1979-05-27\n", "a", TomlValueKind.LocalDate)]
    [DataRow("local time", "a = 07:32:00\n", "a", TomlValueKind.LocalTime)]
    [DataRow("empty array", "a = []\n", "a", TomlValueKind.Array)]
    [DataRow("nested arrays", "a = [[1], []]\n", "a", TomlValueKind.Array)]
    [DataRow("inline table", "a = {x = 1}\n", "a", TomlValueKind.Table)]
    [DataRow("empty inline table", "a = {}\n", "a", TomlValueKind.Table)]
    [DataRow("header table", "[a]\nx = 1\n", "a", TomlValueKind.Table)]
    [DataRow("array of tables", "[[a]]\nx = 1\n", "a", TomlValueKind.Array)]
    public void Parse_WhenInputCoversValueKinds_ShouldReportMemberKind(string testName, string input, string key, TomlValueKind expectedKind)
    {
        _ = testName;
        using var document = TomlDocument.Parse(input);

        Assert.AreEqual(expectedKind, document.RootElement.GetProperty(key).ValueKind);
    }

    /// <summary>
    /// Verifies that an empty document parses into a root table with no pairs, because the TOML root is always a
    /// table.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputEmpty_ShouldExposeEmptyRootTable()
    {
        using var document = TomlDocument.Parse(string.Empty);

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.AreEqual(0, document.RootElement.EnumerateObject().Count());
    }

    /// <summary>
    /// Verifies that a document containing only a comment parses into an empty root table.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsCommentOnly_ShouldExposeEmptyRootTable()
    {
        using var document = TomlDocument.Parse("# only a comment\n");

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.AreEqual(0, document.RootElement.EnumerateObject().Count());
    }

    /// <summary>
    /// Verifies that inline-value nesting equal to the configured <see cref="TomlDocumentOptions.MaxDepth" /> is
    /// accepted, where each inline array level costs one unit of depth.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingEqualsMaxDepth_ShouldSucceed()
    {
        using var document = TomlDocument.Parse("a = [[1]]\n", new TomlDocumentOptions { MaxDepth = 2 });

        Assert.AreEqual(TomlValueKind.Array, document.RootElement.GetProperty("a").ValueKind);
    }

    /// <summary>
    /// Verifies that inline-value nesting deeper than the configured <see cref="TomlDocumentOptions.MaxDepth" />
    /// throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var document = TomlDocument.Parse("a = [[[1]]]\n", new TomlDocumentOptions { MaxDepth = 2 });
        });
    }

    /// <summary>
    /// Verifies that a <see cref="TomlDocumentOptions.MaxDepth" /> of zero or less selects the default maximum depth
    /// instead of rejecting all nesting.
    /// </summary>
    /// <param name="maxDepth">The non-positive depth that selects the default.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Parse_WhenMaxDepthNotPositive_ShouldUseDefaultDepth(int maxDepth)
    {
        using var document = TomlDocument.Parse("a = [[[1]]]\n", new TomlDocumentOptions { MaxDepth = maxDepth });

        Assert.AreEqual(TomlValueKind.Array, document.RootElement.GetProperty("a").ValueKind);
    }

    /// <summary>
    /// Verifies that inline arrays nested exactly to the default maximum depth of 256 are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestingAtDefaultMaxDepth_ShouldParseDocument()
    {
        var atLimit = "a = " + new string('[', 256) + "1" + new string(']', 256) + "\n";

        using var document = TomlDocument.Parse(atLimit);

        Assert.AreEqual(TomlValueKind.Array, document.RootElement.GetProperty("a").ValueKind);
    }

    /// <summary>
    /// Verifies that inline arrays nested one level beyond the default maximum depth of 256 throw
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestingExceedsDefaultMaxDepth_ShouldThrowTomlFormatException()
    {
        var beyondLimit = "a = " + new string('[', 257) + "1" + new string(']', 257) + "\n";

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var deep = TomlDocument.Parse(beyondLimit);
        });
    }

    /// <summary>
    /// Verifies that the <c>\e</c> escape is rejected under the default TOML v1.0 grammar.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEscapeSequenceEscape_ForV10_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var rejected = TomlDocument.Parse("a = \"\\e\"\n");
        });
    }

    /// <summary>
    /// Verifies that the <c>\e</c> escape decodes to U+001B when <see cref="TomlDocumentOptions.SpecVersion" />
    /// selects <see cref="TomlSpecVersion.V1_1" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEscapeSequenceEscape_ForV11_ShouldDecodeEscapeCharacter()
    {
        using var document = TomlDocument.Parse("a = \"\\e\"\n", new TomlDocumentOptions { SpecVersion = TomlSpecVersion.V1_1 });

        Assert.AreEqual("\u001b", document.RootElement.GetProperty("a").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlDocument.Parse(string, TomlDocumentOptions)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>toml</c> when the text is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringIsNull_ForOptionsOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            using var document = TomlDocument.Parse((string)null!, new TomlDocumentOptions { MaxDepth = 4 });
        }, "toml");
    }

    /// <summary>
    /// Verifies that the document decodes scalars during parsing, so mutating the source buffer after
    /// <see cref="TomlDocument.Parse(ReadOnlySpan{byte})" /> returns does not change the values the document exposes.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceMutatedAfterParse_ShouldNotAffectDocument()
    {
        var source = Encoding.UTF8.GetBytes("name = \"spam\"\n");
        using var document = TomlDocument.Parse(source.AsSpan());

        source[9] = (byte)'X';

        Assert.AreEqual("spam", document.RootElement.GetProperty("name").GetString());
    }

    /// <summary>
    /// Verifies that a dotted key declares a nested table reachable through named navigation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDottedKeyDeclared_ShouldExposeNestedTable()
    {
        using var document = TomlDocument.Parse("server.port = 8080\n");

        TomlElement server = document.RootElement.GetProperty("server");
        Assert.AreEqual(TomlValueKind.Table, server.ValueKind);
        Assert.AreEqual(8080L, server.GetProperty("port").GetInt64());
    }
}
