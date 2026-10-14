// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.RootFolder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that reaching the root folder of a reference store truncated to 600 bytes, whose node B-tree lies past
    /// the end of the file, throws <see cref="OutlookPstFormatException" />, keeping the container's
    /// <see cref="PstFileFormatException" /> as the inner exception.
    /// </summary>
    [TestMethod]
    public void RootFolder_WhenFileIsTruncated_ShouldThrowOutlookPstFormatException()
    {
        byte[] truncated = PstReferenceFixtures.OpenStream(Sample1).ToArray().AsSpan(0, 600).ToArray();
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(truncated, writable: false), new OutlookMailStoreReaderOptions());

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.RootFolder;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reaching the root folder of a store that has no root folder node throws
    /// <see cref="OutlookPstFormatException" />, keeping the container's <see cref="PstNodeNotFoundException" /> as the
    /// inner exception.
    /// </summary>
    [TestMethod]
    public void RootFolder_WhenRootFolderNodeIsAbsent_ShouldThrowOutlookPstFormatException()
    {
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput("gen:Ndb(nodes=2)");
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), new OutlookMailStoreReaderOptions());

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.RootFolder;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstNodeNotFoundException>(ex.InnerException);
    }
}
