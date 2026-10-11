// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
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
