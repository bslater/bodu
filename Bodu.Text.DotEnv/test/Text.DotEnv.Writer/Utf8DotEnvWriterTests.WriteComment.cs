// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriterTests.WriteComment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DotEnvWriter.WriteComment(string)" />.
/// </summary>
public partial class Utf8DotEnvWriterTests
{
    /// <summary>
    /// Verifies that comment text holding a line feed, a CRLF and a lone carriage return is written as one comment line
    /// per line of the text, so that no line of it reads back as an entry.
    /// </summary>
    [TestMethod]
    public void WriteComment_WhenTheTextHoldsLineBreaks_ShouldWriteOneCommentLinePerLine()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DotEnvWriter(buffer);

        writer.WriteComment("a\nB=1\r\nc\rd");
        writer.Flush();

        Assert.AreEqual("#a\n#B=1\n#c\n#d\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
