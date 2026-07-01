// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteStartTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteStartTable" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that an empty root table emits no bytes.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenRootTableEmpty_ShouldEmitNothing()
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
    public void WriteStartTable_WhenSubTableEmpty_ShouldEmitBareHeader()
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
    /// Verifies that scalar and array members are emitted as key/value lines before any sub-table header so that no
    /// line is orphaned beneath a later section.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenScalarsFollowedBySubTable_ShouldEmitScalarsFirst()
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
    /// Verifies that a deeply nested table chain renders one dotted-path header per level.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenTablesNestedThreeLevels_ShouldEmitDottedHeaders()
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
    /// Verifies that starting a second root table after the first document has completed throws
    /// <see cref="InvalidOperationException" />, because concatenating two documents onto one destination produces
    /// invalid TOML.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenSecondRootTableWritten_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WriteEndTable();

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlWriter.WriteStartTable(string)" /> and
    /// <see cref="Utf8TomlWriter.WriteStartArray(string)" /> open a named container in one call.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenNamed_ShouldOpenNamedContainers()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteStartArray("ports");
        writer.WriteInteger(1);
        writer.WriteInteger(2);
        writer.WriteEndArray();
        writer.WriteStartTable("server");
        writer.WriteString("host", "a");
        writer.WriteEndTable();
        writer.WriteEndTable();

        Assert.AreEqual(
            "ports = [1, 2]\n\n[server]\nhost = \"a\"\n",
            Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a small document containing a root scalar, a sub-table, an inline array, and an array of tables
    /// is emitted as the expected canonical UTF-8 text.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void WriteStartTable_WhenSmallDocument_ShouldEmitExpectedCanonicalText()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();

        writer.WritePropertyName("name");
        writer.WriteString("x");

        writer.WritePropertyName("nums");
        writer.WriteStartArray();
        writer.WriteInteger(1);
        writer.WriteInteger(2);
        writer.WriteInteger(3);
        writer.WriteEndArray();

        writer.WritePropertyName("table");
        writer.WriteStartTable();
        writer.WritePropertyName("a");
        writer.WriteInteger(1);
        writer.WritePropertyName("b");
        writer.WriteString("y");
        writer.WriteEndTable();

        writer.WritePropertyName("products");
        writer.WriteStartArray();
        writer.WriteStartTable();
        writer.WritePropertyName("name");
        writer.WriteString("A");
        writer.WriteEndTable();
        writer.WriteStartTable();
        writer.WritePropertyName("name");
        writer.WriteString("B");
        writer.WriteEndTable();
        writer.WriteEndArray();

        writer.WriteEndTable();

        string expected =
            "name = \"x\"\n" +
            "nums = [1, 2, 3]\n" +
            "\n" +
            "[table]\n" +
            "a = 1\n" +
            "b = \"y\"\n" +
            "\n" +
            "[[products]]\n" +
            "name = \"A\"\n" +
            "\n" +
            "[[products]]\n" +
            "name = \"B\"\n";

        Assert.AreEqual(expected, s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that each scalar value kind, including every date-time kind, is emitted in its canonical TOML form.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenScalarKinds_ShouldEmitCanonicalForms()
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);

        writer.WriteStartTable();

        writer.WritePropertyName("s");
        writer.WriteString("hi\tthere");

        writer.WritePropertyName("i");
        writer.WriteInteger(-42);

        writer.WritePropertyName("f");
        writer.WriteFloat(1.5);

        writer.WritePropertyName("whole");
        writer.WriteFloat(3.0);

        writer.WritePropertyName("inf");
        writer.WriteFloat(double.PositiveInfinity);

        writer.WritePropertyName("ninf");
        writer.WriteFloat(double.NegativeInfinity);

        writer.WritePropertyName("nan");
        writer.WriteFloat(double.NaN);

        writer.WritePropertyName("b");
        writer.WriteBoolean(true);

        writer.WritePropertyName("odt");
        writer.WriteOffsetDateTime(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));

        writer.WritePropertyName("ldt");
        writer.WriteLocalDateTime(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified));

        writer.WritePropertyName("ld");
        writer.WriteLocalDate(new DateOnly(1979, 5, 27));

        writer.WritePropertyName("lt");
        writer.WriteLocalTime(new TimeOnly(7, 32, 10));

        writer.WriteEndTable();

        string expected =
            "s = \"hi\\tthere\"\n" +
            "i = -42\n" +
            "f = 1.5\n" +
            "whole = 3.0\n" +
            "inf = inf\n" +
            "ninf = -inf\n" +
            "nan = nan\n" +
            "b = true\n" +
            "odt = 1979-05-27T07:32:00Z\n" +
            "ldt = 1979-05-27T07:32:00\n" +
            "ld = 1979-05-27\n" +
            "lt = 07:32:10\n";

        Assert.AreEqual(expected, s_utf8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that opening a container beyond the configured maximum depth throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void WriteStartTable_WhenNestingExceedsMaxDepth_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            TomlWriterOptions options = new() { MaxDepth = 1 };
            Utf8TomlWriter writer = new(buffer, options);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteStartTable();
        });
    }

}
