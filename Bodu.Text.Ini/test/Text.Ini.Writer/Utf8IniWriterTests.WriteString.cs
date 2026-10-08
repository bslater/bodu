// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

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
}
