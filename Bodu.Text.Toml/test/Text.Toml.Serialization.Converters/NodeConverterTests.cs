// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NodeConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Verifies the guard of the shared <see cref="NodeConverter" /> that the serializer's own null handling keeps the
/// public surface from reaching: a <see langword="null" /> node handed to the converter is refused rather than
/// written as nothing.
/// </summary>
[TestClass]
public class NodeConverterTests
{
    /// <summary>
    /// Verifies that writing a <see langword="null" /> node throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenValueIsNull_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            WriteNode(null);
        });
    }

    /// <summary>
    /// Verifies that writing a node writes its TOML, the positive counterpart of the null test.
    /// </summary>
    [TestMethod]
    public void Write_WhenValueIsTable_ShouldWriteItsEntries()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        new NodeConverter().Write(writer, new TomlObject { ["a"] = 1 }, new TomlSerializerOptions());
        writer.Flush();

        Assert.AreEqual("a = 1\n", System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Writes a node through the converter into a discarded buffer.
    /// </summary>
    /// <param name="node">The node, or <see langword="null" />.</param>
    private static void WriteNode(TomlNode? node)
    {
        var writer = new Utf8TomlWriter(new ArrayBufferWriter<byte>());

        new NodeConverter().Write(writer, node!, new TomlSerializerOptions());
    }
}
