// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Layout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that an empty root table emits no bytes.
    /// </summary>
    [TestMethod]
    public void Write_WhenRootTableEmpty_ShouldEmitNothing()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WriteEndTable();
        });

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that an empty sub-table is emitted as a bare <c>[header]</c> with no key/value lines.
    /// </summary>
    [TestMethod]
    public void Write_WhenSubTableEmpty_ShouldEmitBareHeader()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("t");
            writer.WriteStartTable();
            writer.WriteEndTable();
            writer.WriteEndTable();
        });

        Assert.AreEqual("[t]\n", actual);
    }

    /// <summary>
    /// Verifies that an empty array member is emitted inline as <c>[]</c>.
    /// </summary>
    [TestMethod]
    public void Write_WhenArrayEmpty_ShouldEmitInlineEmptyArray()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndTable();
        });

        Assert.AreEqual("a = []\n", actual);
    }

    /// <summary>
    /// Verifies that an array of scalars is emitted inline as a comma-and-space separated list.
    /// </summary>
    [TestMethod]
    public void Write_WhenArrayOfScalars_ShouldEmitInlineArray()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartArray();
            writer.WriteInteger(1);
            writer.WriteString("two");
            writer.WriteBoolean(true);
            writer.WriteEndArray();
            writer.WriteEndTable();
        });

        Assert.AreEqual("a = [1, \"two\", true]\n", actual);
    }

    /// <summary>
    /// Verifies that scalar and array members are emitted as key/value lines before any sub-table header so that no
    /// line is orphaned beneath a later section.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarsFollowedBySubTable_ShouldEmitScalarsFirst()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();

            writer.WritePropertyName("sub");
            writer.WriteStartTable();
            writer.WritePropertyName("x");
            writer.WriteInteger(1);
            writer.WriteEndTable();

            writer.WritePropertyName("top");
            writer.WriteInteger(9);

            writer.WriteEndTable();
        });

        // The top-level scalar is emitted before the [sub] section even though it was written after the sub-table.
        Assert.AreEqual("top = 9\n\n[sub]\nx = 1\n", actual);
    }

    /// <summary>
    /// Verifies that an empty table appearing as an array element is emitted inline as <c>{}</c>.
    /// </summary>
    [TestMethod]
    public void Write_WhenEmptyTableIsArrayElement_ShouldEmitInlineEmptyTable()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteStartTable();
            writer.WriteEndTable();
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndTable();
        });

        Assert.AreEqual("a = [[{}]]\n", actual);
    }

    /// <summary>
    /// Verifies that a run of array-of-tables elements is emitted as repeated <c>[[header]]</c> blocks separated by a
    /// blank line.
    /// </summary>
    [TestMethod]
    public void Write_WhenArrayOfTables_ShouldEmitRepeatedDoubleHeaders()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("item");
            writer.WriteStartArray();

            writer.WriteStartTable();
            writer.WritePropertyName("n");
            writer.WriteInteger(1);
            writer.WriteEndTable();

            writer.WriteStartTable();
            writer.WritePropertyName("n");
            writer.WriteInteger(2);
            writer.WriteEndTable();

            writer.WriteEndArray();
            writer.WriteEndTable();
        });

        Assert.AreEqual("[[item]]\nn = 1\n\n[[item]]\nn = 2\n", actual);
    }

    /// <summary>
    /// Verifies that a deeply nested table chain renders one dotted-path header per level.
    /// </summary>
    [TestMethod]
    public void Write_WhenTablesNestedThreeLevels_ShouldEmitDottedHeaders()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartTable();
            writer.WritePropertyName("b");
            writer.WriteStartTable();
            writer.WritePropertyName("c");
            writer.WriteInteger(1);
            writer.WriteEndTable();
            writer.WriteEndTable();
            writer.WriteEndTable();
        });

        Assert.AreEqual("[a]\n\n[a.b]\nc = 1\n", actual);
    }

    /// <summary>
    /// Verifies that closing an array as the outermost container — leaving a non-table root — emits no document bytes.
    /// </summary>
    /// <remarks>
    /// The canonical writer only serializes when the closed root is a table. Closing an array at the root therefore
    /// records the value but produces no output, mirroring TOML's requirement that a document root be a table.
    /// </remarks>
    [TestMethod]
    public void Write_WhenRootIsArray_ShouldEmitNothing()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartArray();
            writer.WriteInteger(1);
            writer.WriteEndArray();
        });

        Assert.AreEqual(string.Empty, actual);
    }
}
