// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.SpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="TomlWriterOptions.SpecVersion" /> has no effect on the bytes <see cref="Utf8TomlWriter" />
/// emits.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a writer whose <see cref="TomlWriterOptions.SpecVersion" /> is <see cref="TomlSpecVersion.V1_1" />
    /// writes the same bytes as one set to <see cref="TomlSpecVersion.V1_0" />, for a document holding each construct
    /// TOML v1.1.0 could spell differently: the escape character and another control character, a time with zero
    /// seconds, and an inline table.
    /// </summary>
    [TestMethod]
    public void SpecVersion_WhenSetToV11_ShouldWriteSameBytesAsV10()
    {
        byte[] expected = WriteWithSpecVersion(TomlSpecVersion.V1_0);

        byte[] actual = WriteWithSpecVersion(TomlSpecVersion.V1_1);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Writes a document holding each construct TOML v1.1.0 could spell differently, through a writer whose
    /// <see cref="TomlWriterOptions.SpecVersion" /> is <paramref name="specVersion" />.
    /// </summary>
    /// <param name="specVersion">The specification version set on the writer options.</param>
    /// <returns>The UTF-8 bytes the writer emitted.</returns>
    private static byte[] WriteWithSpecVersion(TomlSpecVersion specVersion)
    {
        // SpecVersion is obsolete because it has no effect; setting it anyway is what this test pins.
#pragma warning disable CS0618
        TomlWriterOptions options = new() { SpecVersion = specVersion };
#pragma warning restore CS0618

        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer, options);

        writer.WriteStartTable();
        writer.WriteString("escapes", "\u001b and \u0001");
        writer.WriteLocalTime("time", new TimeOnly(7, 32, 0));
        writer.WritePropertyName("points");
        writer.WriteStartArray();
        writer.WriteStartTable();
        writer.WriteInteger("x", 1);
        writer.WriteInteger("y", 2);
        writer.WriteEndTable();
        writer.WriteInteger(3);
        writer.WriteEndArray();
        writer.WritePropertyName("server");
        writer.WriteStartTable();
        writer.WriteString("host", "localhost");
        writer.WriteEndTable();
        writer.WriteEndTable();

        return buffer.WrittenSpan.ToArray();
    }
}
