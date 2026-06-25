// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the block-style emission of <see cref="Utf8YamlWriter" />.
/// </summary>
[TestClass]
public partial class Utf8YamlWriterTests
{
    /// <summary>A by-ref writer callback used to drive emission within a test.</summary>
    /// <param name="writer">The writer to emit into.</param>
    private delegate void WriterAction(ref Utf8YamlWriter writer);

    /// <summary>Verifies that a simple mapping of scalars is emitted in block style.</summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Write_WhenMappingOfScalars_ShouldEmitBlock()
    {
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("a");
            w.WriteInt64(1);
            w.WritePropertyName("b");
            w.WriteString("two");
            w.WriteEndMapping();
        });

        Assert.AreEqual("a: 1\nb: two\n", yaml);
    }

    /// <summary>Writes via the supplied callback and returns the produced UTF-8 text.</summary>
    /// <param name="write">The emission callback.</param>
    /// <returns>The written YAML text.</returns>
    private static string Write(WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);
        write(ref writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
