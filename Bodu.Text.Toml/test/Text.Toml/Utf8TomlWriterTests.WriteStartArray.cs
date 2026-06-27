// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteStartArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteStartArray" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that an empty array member is emitted inline as <c>[]</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartArray_WhenEmpty_ShouldEmitInlineEmptyArray()
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
    public void WriteStartArray_WhenArrayOfScalars_ShouldEmitInlineArray()
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
    /// Verifies that an empty table appearing as an array element is emitted inline as <c>{}</c>.
    /// </summary>
    [TestMethod]
    public void WriteStartArray_WhenEmptyTableIsArrayElement_ShouldEmitInlineEmptyTable()
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
    public void WriteStartArray_WhenArrayOfTables_ShouldEmitRepeatedDoubleHeaders()
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
    /// Verifies that completing a root-level array throws <see cref="InvalidOperationException" />, because the root
    /// of a TOML document must be a table; the writer must not complete silently without emitting output.
    /// </summary>
    [TestMethod]
    public void WriteStartArray_WhenRootValueIsArray_ShouldThrowInvalidOperationException()
    {
        ArrayBufferWriter<byte> buffer = new();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlWriter writer = new(buffer);
            writer.WriteStartArray();
            writer.WriteEndArray();
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that a table reached inside a nested array — where the enclosing member is a plain array rather than an
    /// array of tables — is emitted in inline <c>{ … }</c> form, and the nested array stays inline.
    /// </summary>
    /// <remarks>
    /// A direct array-of-tables member surfaces as <c>[[header]]</c> blocks; only a table that appears within a value
    /// formatted as an inline array renders inline. Wrapping the table array in an outer array forces that inline path.
    /// </remarks>
    [TestMethod]
    public void WriteStartArray_WhenTableInsideNestedArray_ShouldEmitInlineTable()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("points");
        writer.WriteStartArray();
        writer.WriteStartArray();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(1);
        writer.WritePropertyName("y");
        writer.WriteInteger(2);
        writer.WriteEndTable();

        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.WriteEndTable();

        Assert.AreEqual("points = [[{ x = 1, y = 2 }]]\n", s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a top-level array whose every element is a table is emitted as a run of <c>[[header]]</c>
    /// array-of-tables blocks rather than as an inline array.
    /// </summary>
    [TestMethod]
    public void WriteStartArray_WhenArrayOfTablesAtRoot_ShouldEmitDoubleBracketHeaders()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();
        writer.WritePropertyName("points");
        writer.WriteStartArray();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(1);
        writer.WriteEndTable();

        writer.WriteStartTable();
        writer.WritePropertyName("x");
        writer.WriteInteger(2);
        writer.WriteEndTable();

        writer.WriteEndArray();
        writer.WriteEndTable();

        Assert.AreEqual(
            "[[points]]\nx = 1\n\n[[points]]\nx = 2\n",
            s_utf8.GetString(buffer.WrittenSpan));
    }

}
