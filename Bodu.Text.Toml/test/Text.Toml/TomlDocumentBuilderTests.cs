// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="TomlDocumentBuilder" />, the structural TOML parser, with particular focus on
/// the lexical/structural validation split: every rule enforced by the builder's identity sets must be rejected by the
/// builder even though the same input lexes cleanly.
/// </summary>
[TestClass]
public sealed class TomlDocumentBuilderTests
{
    /// <summary>
    /// Verifies that a structurally invalid document lexes cleanly but is rejected by the builder, proving the
    /// validation split.
    /// </summary>
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
    /// Verifies that out-of-line headers merge their members into nested position in the materialized tree.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOutOfLineHeaders_ShouldMergeIntoNestedTables()
    {
        TomlTableNode root = Parse("[server]\nhost = \"a\"\n\n[client]\ntimeout = 5\n\n[server.tls]\nenabled = true\n");

        Assert.AreEqual(2, root.Items.Count);
        Assert.AreEqual("server", root.Items[0].Key);
        Assert.AreEqual("client", root.Items[1].Key);

        var server = (TomlTableNode)root.Items[0].Value;
        Assert.AreEqual(2, server.Items.Count);
        Assert.AreEqual("host", server.Items[0].Key);
        Assert.AreEqual("tls", server.Items[1].Key);

        var tls = (TomlTableNode)server.Items[1].Value;
        Assert.AreEqual(true, ((TomlScalarNode)tls.Items[0].Value).Value);
    }

    /// <summary>
    /// Verifies that an array-of-tables materializes as an array whose elements are tables, appending across headers.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArrayOfTables_ShouldAppendElements()
    {
        TomlTableNode root = Parse("[[p]]\nn = 1\n\n[[p]]\nn = 2\n");

        var array = (TomlArrayNode)root.Items[0].Value;
        Assert.AreEqual(2, array.Count);
        Assert.AreEqual(1L, ((TomlScalarNode)((TomlTableNode)array.Items[0]).Items[0].Value).Value);
        Assert.AreEqual(2L, ((TomlScalarNode)((TomlTableNode)array.Items[1]).Items[0].Value).Value);
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
        TomlTableNode root = new TomlDocumentBuilder(TomlSpecVersion.V1_0, 3).Parse(Encoding.UTF8.GetBytes("a = [[[1]]]\n"));

        Assert.AreEqual(1, root.Items.Count);
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

        TomlTableNode root = new TomlDocumentBuilder(TomlSpecVersion.V1_0, int.MaxValue).Parse(Encoding.UTF8.GetBytes(toml));

        Assert.AreEqual(1, root.Items.Count);
    }

    /// <summary>
    /// Verifies that node offsets in the materialized tree are byte offsets into the UTF-8 source.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultiByteContent_ShouldCarryByteOffsets()
    {
        // Bytes: s(0) sp(1) =(2) sp(3) "(4) é(5,6) "(7) LF(8) i(9) sp(10) =(11) sp(12) 1(13) LF(14).
        TomlTableNode root = Parse("s = \"é\"\ni = 1\n");

        Assert.AreEqual(4, ((TomlScalarNode)root.Items[0].Value).Offset);
        Assert.AreEqual(13, ((TomlScalarNode)root.Items[1].Value).Offset);
    }

    /// <summary>
    /// Parses <paramref name="toml" /> with default options and returns the root table.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <returns>The materialized root table.</returns>
    private static TomlTableNode Parse(string toml) =>
        new TomlDocumentBuilder(TomlSpecVersion.V1_0, 256).Parse(Encoding.UTF8.GetBytes(toml));
}
