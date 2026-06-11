// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.InlineTables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that an empty inline table surfaces as adjacent start and end table boundaries.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableIsEmpty_ShouldSurfaceEmptyTable()
    {
        TomlDocumentReader reader = Create("v = {}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that an inline table with several pairs surfaces each key/value in declared order.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasMultiplePairs_ShouldSurfaceEachPair()
    {
        TomlDocumentReader reader = Create("v = {x = 1, y = 2, z = 3}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectProperty(ref reader, "y");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectProperty(ref reader, "z");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that an inline table nested within another inline table surfaces as nested table boundaries.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableIsNested_ShouldSurfaceNestedTable()
    {
        TomlDocumentReader reader = Create("v = {point = {x = 1, y = 2}}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "point");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectProperty(ref reader, "y");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that an inline table holding a dotted key surfaces the implied intermediate table.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasDottedKey_ShouldSurfaceIntermediateTable()
    {
        TomlDocumentReader reader = Create("v = {a.b = 1}\n");

        ExpectSingleValue(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
    }

    /// <summary>
    /// Verifies that a trailing comma in an inline table is rejected under TOML v1.0.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasTrailingComma_ForV10_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = {x = 1,}\n");
        });
    }

    /// <summary>
    /// Verifies that defining the same key twice inside an inline table is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableHasDuplicateKey_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = {x = 1, x = 2}\n");
        });
    }

    /// <summary>
    /// Verifies that a single-line inline table that is not closed before the end of the line is rejected under TOML
    /// v1.0.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableSpansLines_ForV10_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = {x = 1,\ny = 2}\n");
        });
    }

    /// <summary>
    /// Verifies that extending a previously closed inline table with a later header is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderExtendsInlineTable_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = {b = 1}\n[a.c]\nd = 2\n");
        });
    }
}
