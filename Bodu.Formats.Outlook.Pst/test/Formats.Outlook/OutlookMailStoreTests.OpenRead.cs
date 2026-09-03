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
    /// Verifies that opening the reference fixture and walking to the oracle folder and message succeeds — the
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
    /// Verifies that opening a stream that is not a PST file surfaces the container's format exception.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenStreamIsNotPst_ShouldThrowExactly()
    {
        using var stream = new MemoryStream(new byte[1024]);

        _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            using OutlookMailStore store = OutlookMailStore.OpenRead(stream);
        });
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
