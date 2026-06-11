// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Tables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that an empty <c>[table]</c> header surfaces as an empty nested table.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyTableHeader_ShouldSurfaceEmptyTable()
    {
        TomlDocumentReader reader = Create("[empty]\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "empty");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a deeply nested <c>[a.b.c]</c> header surfaces as three levels of nested tables.
    /// </summary>
    [TestMethod]
    public void Read_WhenThreeLevelHeader_ShouldSurfaceThreeNestedTables()
    {
        TomlDocumentReader reader = Create("[a.b.c]\nd = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3, reader.CurrentDepth);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that defining a super-table header after its sub-table merges the later keys into the same table.
    /// </summary>
    [TestMethod]
    public void Read_WhenSuperTableDefinedAfterSubTable_ShouldMergeKeys()
    {
        TomlDocumentReader reader = Create("[a.b.c]\nd = 1\n\n[a]\ne = 2\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);

        // The sub-table b.c.d declared first.
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);

        // The key promoted onto the super-table a by the later [a] header.
        ExpectProperty(ref reader, "e");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());

        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a dotted key under a <c>[table]</c> header creates nested sub-tables beneath that table.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKeyUnderHeader_ShouldNestUnderHeaderTable()
    {
        TomlDocumentReader reader = Create("[a]\nb.c = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that two array-of-tables headers of the same path append a second element to the same array.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayOfTablesAppended_ShouldGrowSameArray()
    {
        TomlDocumentReader reader = Create("[[fruit]]\nname = \"apple\"\n\n[[fruit]]\nname = \"banana\"\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "fruit");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("apple", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("banana", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a sub-table header nested beneath an array-of-tables element attaches to the most recent element.
    /// </summary>
    [TestMethod]
    public void Read_WhenSubTableUnderArrayOfTables_ShouldNestUnderLastElement()
    {
        TomlDocumentReader reader = Create("[[fruit]]\nname = \"apple\"\n\n[fruit.physical]\ncolor = \"red\"\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "fruit");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("apple", reader.GetString());

        ExpectProperty(ref reader, "physical");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "color");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("red", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that conflicting and duplicate table declarations are rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="source">The malformed table document under test.</param>
    [TestMethod]
    [DataRow("[a]\n[a]\n", DisplayName = "duplicate table")]
    [DataRow("[a]\nb = 1\n[a.b]\nc = 2\n", DisplayName = "header over existing value")]
    [DataRow("a.b = 1\n[a]\nc = 2\n", DisplayName = "header over dotted table")]
    [DataRow("[a]\n[a.b]\n[a]\n", DisplayName = "redefine explicit super-table")]
    [DataRow("[fruit]\n[[fruit]]\n", DisplayName = "array-of-tables over table")]
    [DataRow("v = []\n[[v]]\n", DisplayName = "array-of-tables over static array")]
    [DataRow("[a\n", DisplayName = "unterminated header")]
    [TestCategory("Regression")]
    public void Read_WhenTableDeclarationInvalid_ShouldThrowTomlFormatException(string source)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(source);
        });
    }
}
