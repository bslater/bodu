// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.WriteEndObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedWriter.WriteEndObject" />.
/// </summary>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that with <see cref="DelimitedWriterOptions.NoHeader" /> set, object records write only their value rows,
    /// the header row their names would supply being suppressed.
    /// </summary>
    [TestMethod]
    public void WriteEndObject_WhenNoHeader_ShouldEmitOnlyValueRows()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { NoHeader = true });

        writer.WriteStartArray();
        WriteRecord(ref writer, ("C1", "a"), ("C2", "b"), ("C3", "c"));
        WriteRecord(ref writer, ("C1", "x"), ("C2", "y"), ("C3", "z"));
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("a,b,c\r\nx,y,z\r\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that the header row follows the same quoting rules as a value row: a first name beginning with the
    /// comment character and a name with a space at either end are quoted.
    /// </summary>
    [TestMethod]
    public void WriteEndObject_WhenHeaderNamesWouldReadBackDifferently_ShouldQuoteThem()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("#id", "1"), (" name ", "Ada"));
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("\"#id\",\" name \"\r\n1,Ada\r\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
