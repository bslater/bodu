// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test.IO;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that opening a store whose header carries the OST client magic <c>SO</c> propagates the container's
    /// <see cref="PstUnsupportedFormatException" /> whatever the header's version, as the guides document.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="version">The header's <c>wVer</c>.</param>
    [TestMethod]
    [DataRow("version 23", 23)]
    [DataRow("version 36", 36)]
    public void Open_WhenClientMagicIsOst_ShouldThrowPstUnsupportedFormatException(string testName, int version)
    {
        Assert.IsNotNull(testName);
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput($"gen:Mail()|Header(client=0x4F53,version={version})");

        var ex = Assert.ThrowsExactly<PstUnsupportedFormatException>(() =>
        {
            using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), new OutlookMailStoreReaderOptions());
        });

        Assert.AreEqual(PstFileError.UnsupportedFormat, ex.Error);
    }

    /// <summary>
    /// Verifies that a store read through a stream that returns at most one byte per read renders exactly as the same
    /// bytes read from a <see cref="MemoryStream" />, so no read assumes the source fills the buffer it is given.
    /// </summary>
    [TestMethod]
    public void Open_WhenTheStreamReturnsOneByteAtATime_ShouldReadTheStore()
    {
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput("gen:Mail(att=1~a.txt~hello)");
        string expected = Render(new MemoryStream(input, writable: false));

        string actual = Render(new FixedChunkStream(input, chunkSize: 1));

        StringAssert.Contains(expected, "#attachment\t0\tByValue\ta.txt\t5\t68656C6C6F", "The attachment content must be read.");
        Assert.AreEqual(expected, actual);

        static string Render(Stream stream)
        {
            using OutlookMailStore store = OutlookMailStore.Open(stream, new OutlookMailStoreReaderOptions());
            return PstReleaseNoteCorpusTests.RenderStore(store, "Detail=attachments");
        }
    }
}
