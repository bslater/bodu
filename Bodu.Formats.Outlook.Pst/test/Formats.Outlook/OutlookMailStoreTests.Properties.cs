// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that the store properties decode from the message-store node, are decoded once, and carry the
    /// store display name.
    /// </summary>
    [TestMethod]
    public void Properties_WhenReferenceFixture_ShouldDecodeStoreObject()
    {
        using OutlookMailStore store = OpenSample1();

        MapiPropertyCollection properties = store.Properties;

        Assert.IsTrue(properties.Count > 0, "The store object must carry decoded properties.");
        Assert.AreSame(properties, store.Properties, "The store properties must decode once and be cached.");
        Assert.AreEqual(store.Properties.GetString(MapiPropertyIds.DisplayName), store.DisplayName);
    }

    /// <summary>
    /// Verifies that the store decodes its properties under strict validation as well as the tolerant levels.
    /// </summary>
    [TestMethod]
    public void Properties_WhenStrictValidation_ShouldDecodeStoreObject()
    {
        using OutlookMailStore store = OpenSample1(PstValidationLevel.Strict);

        Assert.IsTrue(store.Properties.Count > 0);
    }

    /// <summary>
    /// Verifies that reading the properties of a reference store truncated to 600 bytes, which opens because its header
    /// is intact, throws <see cref="OutlookPstFormatException" />, keeping the container's
    /// <see cref="PstFileFormatException" /> as the inner exception, so a handler for the reader's family catches it.
    /// </summary>
    [TestMethod]
    public void Properties_WhenFileIsTruncated_ShouldThrowOutlookPstFormatException()
    {
        byte[] truncated = PstReferenceFixtures.OpenStream(Sample1).ToArray().AsSpan(0, 600).ToArray();
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(truncated, writable: false), new OutlookMailStoreReaderOptions());

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.Properties;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reading the properties of a store whose message-store object has a corrupt heap throws
    /// <see cref="OutlookPstFormatException" />, keeping the container's <see cref="PstFileFormatException" /> as the
    /// inner exception.
    /// </summary>
    [TestMethod]
    public void Properties_WhenStoreObjectHeapIsCorrupt_ShouldThrowOutlookPstFormatException()
    {
        byte[] input = GenerateWithCorruptHeap("gen:Mail()", 0x21);
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), new OutlookMailStoreReaderOptions());

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = store.Properties;
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<PstFileFormatException>(ex.InnerException);
        Assert.AreEqual(PstFileError.InvalidHeap, ((PstFileFormatException)ex.InnerException).Error);
    }

    /// <summary>
    /// Verifies that a store whose message-store object node is absent still exposes its folder hierarchy - the
    /// folders decode under the fallback encoding instead of failing on the missing node - and reports an empty
    /// store property collection.
    /// </summary>
    [TestMethod]
    public void RootFolder_WhenStoreObjectAbsent_ShouldStillDecodeFolders()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.IncludeStoreObject = false);

        Assert.AreEqual(0, store.Properties.Count);
        Assert.IsNull(store.DisplayName);
        Assert.AreEqual("Root Container", store.RootFolder.DisplayName);
        Assert.AreEqual(Bodu.Formats.Outlook.Pst.PstMessagingFixtureBuilder.InboxDisplayName, store.RootFolder.EnumerateSubfolders().Single().DisplayName);
    }
}
