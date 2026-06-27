// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteProperties.cs" company="Bodu Pty. Ltd.">
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
/// Verifies that <see cref="Utf8TomlWriter.WriteProperties" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that the combined property/value methods produce the same document as separate
    /// <c>WritePropertyName</c> and value calls.
    /// </summary>
    [TestMethod]
    public void WriteProperties_WhenEveryPairedOverload_ShouldMatchSeparateCalls()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteString("s", "text");
        writer.WriteInteger("i", 42);
        writer.WriteFloat("f", 1.5);
        writer.WriteBoolean("b", true);
        writer.WriteOffsetDateTime("odt", new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));
        writer.WriteLocalDateTime("ldt", new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified));
        writer.WriteLocalDate("ld", new DateOnly(1979, 5, 27));
        writer.WriteLocalTime("lt", new TimeOnly(7, 32, 0));
        writer.WriteEndTable();

        Assert.AreEqual(
            "s = \"text\"\ni = 42\nf = 1.5\nb = true\nodt = 1979-05-27T07:32:00Z\nldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00\n",
            Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

}
