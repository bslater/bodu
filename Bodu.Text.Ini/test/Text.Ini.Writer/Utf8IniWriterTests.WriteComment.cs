// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WriteComment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniWriter.WriteComment(string)" />.
/// </summary>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a comment is written with the configured prefix.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenCustomPrefix_ShouldUseIt()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer, new IniWriterOptions { CommentPrefix = '#' });

        writer.WriteComment(" a note");
        writer.Flush();

        Assert.AreEqual("# a note\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
