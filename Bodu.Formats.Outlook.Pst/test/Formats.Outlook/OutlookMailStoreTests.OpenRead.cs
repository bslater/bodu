// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that opening the reference fixture and walking to the oracle folder and message succeeds - the
    /// happy-path smoke over the primary public type.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void OpenRead_WhenReferenceFixture_ShouldWalkToOracleMessage()
    {
        using OutlookMailStore store = OpenSample1();

        OutlookMailFolder? sample = Walk(store.RootFolder).FirstOrDefault(f => f.DisplayName == "Sample1");
        Assert.IsNotNull(sample, "The oracle folder 'Sample1' must be reachable from the root.");

        OutlookMailMessage message = sample.EnumerateMessages().Single();
        Assert.AreEqual("Here is a sample message", message.Subject);
        Assert.AreEqual("Terry Mahaffey", message.SenderName);
    }

    /// <summary>
    /// Verifies that opening a stream that is not a PST file throws <see cref="OutlookPstFormatException" />, keeping the
    /// container's <see cref="PstFileFormatException" /> as the inner exception, so one handler for the reader's family
    /// catches it.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenStreamIsNotPst_ShouldThrowOutlookPstFormatException()
    {
        using var stream = new MemoryStream(new byte[1024]);

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            using OutlookMailStore store = OutlookMailStore.OpenRead(stream);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
        Assert.AreEqual(PstFileError.InvalidHeader, ((PstFileFormatException)ex.InnerException).Error);
    }

    /// <summary>
    /// Verifies that a store embedded in a larger stream opens from the stream's current position, as the guides
    /// document, and renders as the same store read on its own.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenStreamIsPositionedPastTheStart_ShouldReadTheStoreFromThatPosition()
    {
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput("gen:Mail()");
        var embedded = new byte[100 + input.Length];
        embedded.AsSpan(0, 100).Fill(0xCC);
        input.CopyTo(embedded, 100);
        string expected;
        using (OutlookMailStore alone = OutlookMailStore.OpenRead(new MemoryStream(input, writable: false)))
            expected = PstReleaseNoteCorpusTests.RenderStore(alone, string.Empty);

        using var stream = new MemoryStream(embedded, writable: false) { Position = 100 };
        using OutlookMailStore store = OutlookMailStore.OpenRead(stream);

        Assert.AreEqual(expected, PstReleaseNoteCorpusTests.RenderStore(store, string.Empty));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> path or stream is rejected.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenArgumentIsNull_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using OutlookMailStore store = OutlookMailStore.OpenRead((string)null!);
        });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using OutlookMailStore store = OutlookMailStore.OpenRead((Stream)null!);
        });
    }
}
