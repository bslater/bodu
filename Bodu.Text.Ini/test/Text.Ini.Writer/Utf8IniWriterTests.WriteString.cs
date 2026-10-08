// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniWriter.WriteString(string)" />.
/// </summary>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a value containing a line break throws <see cref="ArgumentException" /> naming the value, because
    /// one <c>key=value</c> line cannot hold it.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [TestMethod]
    [DataRow("a\nb", DisplayName = "LF")]
    [DataRow("a\rb", DisplayName = "CR")]
    [DataRow("a\r\nb", DisplayName = "CRLF")]
    [DataRow("a\n", DisplayName = "trailing LF")]
    public void WriteString_WhenValueContainsLineBreak_ShouldThrowArgumentException(string value)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8IniWriter(new ArrayBufferWriter<byte>());
            writer.WritePropertyName("key");
            writer.WriteString(value);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a value beginning or ending with a space or a tab throws <see cref="ArgumentException" /> naming
    /// the value, because the reader trims that whitespace and would read back a different value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [TestMethod]
    [DataRow(" a", DisplayName = "leading space")]
    [DataRow("a ", DisplayName = "trailing space")]
    [DataRow("\ta", DisplayName = "leading tab")]
    [DataRow("a\t", DisplayName = "trailing tab")]
    [DataRow("   ", DisplayName = "only spaces")]
    [DataRow("  val ue1 ", DisplayName = "go-ini #260")]
    public void WriteString_WhenValueHasSurroundingWhitespace_ShouldThrowArgumentException(string value)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8IniWriter(new ArrayBufferWriter<byte>());
            writer.WritePropertyName("key");
            writer.WriteString(value);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty value, and a value with whitespace only inside it, are written as they are.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="expected">The text written.</param>
    [TestMethod]
    [DataRow("", "key=\n", DisplayName = "empty")]
    [DataRow("val ue1", "key=val ue1\n", DisplayName = "inner space")]
    [DataRow("a\tb", "key=a\tb\n", DisplayName = "inner tab")]
    public void WriteString_WhenValueIsEmptyOrHasInnerWhitespace_ShouldWriteItUnchanged(string value, string expected)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WritePropertyName("key");
        writer.WriteString(value);
        writer.Flush();

        Assert.AreEqual(expected, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
