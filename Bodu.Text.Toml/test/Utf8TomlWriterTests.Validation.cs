// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that writing the same property name twice within one table throws
    /// <see cref="InvalidOperationException" />, because duplicate keys make a TOML document invalid and the writer
    /// must never emit output its own reader rejects.
    /// </summary>
    [TestMethod]
    public void Write_WhenDuplicatePropertyNameInTable_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteInteger(1);
            writer.WritePropertyName("a");
            writer.WriteInteger(2);
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that writing a value into a table without first supplying a property name throws
    /// <see cref="InvalidOperationException" /> at the offending call, rather than deferring failure to emit time.
    /// </summary>
    [TestMethod]
    public void Write_WhenValueWrittenWithoutPropertyName_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that completing a root-level scalar value throws <see cref="InvalidOperationException" />, because the
    /// root of a TOML document must be a table; the writer must not complete silently without emitting output.
    /// </summary>
    [TestMethod]
    public void Write_WhenRootValueIsScalar_ShouldThrowInvalidOperationException()
    {
        ArrayBufferWriter<byte> buffer = new();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlWriter writer = new(buffer);
            writer.WriteString("x");
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that completing a root-level array throws <see cref="InvalidOperationException" />, because the root
    /// of a TOML document must be a table; the writer must not complete silently without emitting output.
    /// </summary>
    [TestMethod]
    public void Write_WhenRootValueIsArray_ShouldThrowInvalidOperationException()
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
    /// Verifies that starting a second root table after the first document has completed throws
    /// <see cref="InvalidOperationException" />, because concatenating two documents onto one destination produces
    /// invalid TOML.
    /// </summary>
    [TestMethod]
    public void Write_WhenSecondRootTableWritten_ShouldThrowInvalidOperationException()
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
    /// Verifies that supplying a property name while the innermost open container is an array throws
    /// <see cref="InvalidOperationException" />, mirroring the structural validation contract of
    /// <c>Utf8JsonWriter</c>.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WritePropertyName("a");
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Utf8TomlWriter.WritePropertyName" /> twice in a row throws
    /// <see cref="InvalidOperationException" /> rather than silently discarding the first pending name.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenCalledTwiceConsecutively_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WritePropertyName("b");
        });
    }

    /// <summary>
    /// Verifies that closing a table while the innermost open container is an array throws
    /// <see cref="InvalidOperationException" /> rather than an implementation-detail cast failure.
    /// </summary>
    [TestMethod]
    public void WriteEndTable_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that a string value containing an unpaired surrogate throws <see cref="ArgumentException" /> at the
    /// <see cref="Utf8TomlWriter.WriteString" /> call that supplied it, rather than deferring the failure to the
    /// root-table close.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenValueContainsLoneSurrogate_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("s");
            writer.WriteString("a\uD800b");
        });
    }
}
