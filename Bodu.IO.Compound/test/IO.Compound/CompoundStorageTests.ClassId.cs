// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.ClassId.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that a CLSID set on the root and a child storage persists across commit and reopen, visible through
    /// the property, <see cref="CompoundStorage.Stat" />, and the parent's entry enumeration.
    /// </summary>
    [TestMethod]
    public void ClassId_WhenSetOnWritableStorage_ShouldPersistAcrossCommitAndReopen()
    {
        var rootId = new Guid("00020820-0000-0000-c000-000000000046");
        var childId = new Guid("11111111-2222-3333-4444-555555555555");
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.ClassId = rootId;
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.ClassId = childId;
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.AreEqual(rootId, reopened.RootStorage.ClassId);
        Assert.AreEqual(rootId, reopened.RootStorage.Stat.ClassId);

        CompoundStorage storage = reopened.RootStorage.OpenStorage("Embedded");
        Assert.AreEqual(childId, storage.ClassId);
        Assert.AreEqual(childId, reopened.RootStorage.EnumerateEntries().Single(e => e.Name == "Embedded").ClassId);
    }

    /// <summary>
    /// Verifies that reading the CLSID of a storage in a read-only file matches its <see cref="CompoundStorage.Stat" />
    /// snapshot.
    /// </summary>
    [TestMethod]
    public void ClassId_WhenReadOnReadOnlyRoot_ShouldMatchStat()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        Assert.AreEqual(file.RootStorage.Stat.ClassId, file.RootStorage.ClassId);
        Assert.AreNotEqual(Guid.Empty, file.RootStorage.ClassId);
    }

    /// <summary>
    /// Verifies that setting the CLSID on a read-only file throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ClassId_WhenSetOnReadOnlyFile_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.RootStorage.ClassId = Guid.NewGuid();
        });
    }

    /// <summary>
    /// Verifies that setting the CLSID marks the owning file dirty.
    /// </summary>
    [TestMethod]
    public void ClassId_WhenSet_ShouldMarkFileDirty()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        file.RootStorage.ClassId = Guid.NewGuid();

        Assert.IsTrue(file.IsDirty);
    }
}
