// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="TomlDocumentBuilder" />, the structural TOML parser, with particular focus on
/// the lexical/structural validation split: every rule enforced by the builder's flag-and-link row store must be
/// rejected by the builder even though the same input lexes cleanly.
/// </summary>
[TestClass]
public sealed class TomlDocumentBuilderTests
{
    /// <summary>
    /// Verifies that a structurally invalid document lexes cleanly but is rejected by the builder, proving the
    /// validation split.
    /// </summary>
    /// <param name="toml">The structurally invalid TOML source.</param>
    [TestMethod]
    [DataRow("a = 1\na = 2\n", DisplayName = "duplicate key")]
    [DataRow("a.b = 1\na.b = 2\n", DisplayName = "duplicate dotted key")]
    [DataRow("[a]\n[a]\n", DisplayName = "duplicate table header")]
    [DataRow("a.b = 1\n[a.b]\n", DisplayName = "header reopening dotted table")]
    [DataRow("[a]\nb = 1\n[a.b]\n", DisplayName = "header on value key")]
    [DataRow("a = 1\n[a]\n", DisplayName = "table header on scalar key")]
    [DataRow("a = {x = 1}\n[a]\n", DisplayName = "header reopening inline table")]
    [DataRow("a = {x = 1}\na.y = 2\n", DisplayName = "dotted key extending inline table")]
    [DataRow("a = {x = 1}\n[a.b]\n", DisplayName = "header extending inline table")]
    [DataRow("a = [1]\n[[a]]\n", DisplayName = "array-table appending to static array")]
    [DataRow("a = 1\n[[a]]\n", DisplayName = "array-table on scalar key")]
    [DataRow("a.b = 1\na.b.c = 2\n", DisplayName = "dotted key through scalar")]
    public void Parse_WhenStructurallyInvalid_ShouldLexCleanlyAndFailInBuilder(string toml)
    {
        var source = Encoding.UTF8.GetBytes(toml);

        // The lexer accepts the document: the rule under test is structural, not lexical.
        var lexer = new Utf8TomlReader(source, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });
        while (lexer.Read())
        {
        }

        // The builder rejects it, carrying a position from the offending token.
        var ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new TomlDocumentBuilder(TomlSpecVersion.V1_0, 256).Parse(source);
        });

        Assert.IsNotNull(ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that out-of-line headers merge their members into nested position, preserving insertion order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOutOfLineHeaders_ShouldMergeIntoNestedTables()
    {
        using var document = TomlDocument.Parse("[server]\nhost = \"a\"\n\n[client]\ntimeout = 5\n\n[server.tls]\nenabled = true\n");
        TomlElement root = document.RootElement;

        CollectionAssert.AreEqual(new[] { "server", "client" }, Keys(root));

        TomlElement server = root.GetProperty("server");
        CollectionAssert.AreEqual(new[] { "host", "tls" }, Keys(server));
        Assert.AreEqual("a", server.GetProperty("host").GetString());
        Assert.IsTrue(server.GetProperty("tls").GetProperty("enabled").GetBoolean());
    }

    /// <summary>
    /// Verifies that an array-of-tables materializes as an array whose elements are tables, appending across headers.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArrayOfTables_ShouldAppendElements()
    {
        using var document = TomlDocument.Parse("[[p]]\nn = 1\n\n[[p]]\nn = 2\n");
        TomlElement p = document.RootElement.GetProperty("p");

        Assert.AreEqual(TomlValueKind.Array, p.ValueKind);
        Assert.AreEqual(2, p.GetArrayLength());
        Assert.AreEqual(1L, p[0].GetProperty("n").GetInt64());
        Assert.AreEqual(2L, p[1].GetProperty("n").GetInt64());
    }

    /// <summary>
    /// Verifies that nesting beyond the configured maximum depth is rejected by the builder.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new TomlDocumentBuilder(TomlSpecVersion.V1_0, 3).Parse(Encoding.UTF8.GetBytes("a = [[[[1]]]]\n"));
        });

        Assert.IsTrue(ex.Message.Contains("3", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that nesting at exactly the configured maximum depth is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingAtMaxDepth_ShouldSucceed()
    {
        using var document = TomlDocument.Parse("a = [[[1]]]\n", new TomlDocumentOptions { MaxDepth = 3 });

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.IsTrue(document.RootElement.TryGetProperty("a", out _));
    }

    /// <summary>
    /// Verifies that nesting beyond the absolute depth ceiling is rejected even when the configured maximum depth is far
    /// larger, so an unbounded configured depth cannot drive the parser into a <see cref="StackOverflowException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowTomlFormatException()
    {
        var depth = TomlLimits.AbsoluteMaxDepth + 1;
        var toml = "a = " + new string('[', depth) + "1" + new string(']', depth) + "\n";

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new TomlDocumentBuilder(TomlSpecVersion.V1_0, int.MaxValue).Parse(Encoding.UTF8.GetBytes(toml));
        });
    }

    /// <summary>
    /// Verifies that nesting at exactly the absolute depth ceiling is accepted, confirming the cap bounds rather than
    /// lowers the limit.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingAtAbsoluteCap_ShouldSucceed()
    {
        var depth = TomlLimits.AbsoluteMaxDepth;
        var toml = "a = " + new string('[', depth) + "1" + new string(']', depth) + "\n";

        using var document = TomlDocument.Parse(toml, new TomlDocumentOptions { MaxDepth = int.MaxValue });

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.IsTrue(document.RootElement.TryGetProperty("a", out _));
    }

    /// <summary>
    /// Verifies that the row store carries byte offsets into the UTF-8 source for each scalar.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultiByteContent_ShouldCarryByteOffsets()
    {
        // Bytes: s(0) sp(1) =(2) sp(3) "(4) é(5,6) "(7) LF(8) i(9) sp(10) =(11) sp(12) 1(13) LF(14).
        TomlReaderRow[] rows = new TomlDocumentBuilder(TomlSpecVersion.V1_0, 256).Parse(Encoding.UTF8.GetBytes("s = \"é\"\ni = 1\n"));

        var first = rows[rows[0].FirstChild];
        var second = rows[first.NextSibling];

        Assert.AreEqual(4, first.Offset);
        Assert.AreEqual(13, second.Offset);
    }

    /// <summary>
    /// Collects the property names of a table element in stored order.
    /// </summary>
    /// <param name="table">The table element to read.</param>
    /// <returns>The property names in order.</returns>
    private static string[] Keys(TomlElement table)
    {
        var names = new List<string>();
        foreach (TomlProperty property in table.EnumerateObject())
            names.Add(property.Name);

        return [.. names];
    }
}
