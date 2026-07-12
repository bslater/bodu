// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.CreationTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that a creation time set on the root and a child storage persists across commit and reopen, visible
    /// through the property, <see cref="CompoundStorage.Stat" />, and the parent's entry enumeration.
    /// </summary>
    [TestMethod]
    public void CreationTime_WhenSetOnWritableStorage_ShouldPersistAcrossCommitAndReopen()
    {
        var rootTime = new DateTimeOffset(2021, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var childTime = new DateTimeOffset(2022, 8, 9, 10, 11, 12, TimeSpan.Zero);
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreationTime = rootTime;
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.CreationTime = childTime;
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.AreEqual(rootTime, reopened.RootStorage.CreationTime);
        Assert.AreEqual(rootTime, reopened.RootStorage.Stat.CreationTime);

        CompoundStorage storage = reopened.RootStorage.OpenStorage("Embedded");
        Assert.AreEqual(childTime, storage.CreationTime);
        Assert.AreEqual(childTime, reopened.RootStorage.EnumerateEntries().Single(e => e.Name == "Embedded").CreationTime);
    }

    /// <summary>
    /// Verifies that a storage created through the edit surface carries no creation time unless one is set, keeping
    /// the value unstamped by <see cref="CompoundFile.Commit" />.
    /// </summary>
    [TestMethod]
    public void CreationTime_WhenNeverSet_ShouldRemainNullAfterCommit()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStorage("Embedded");
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsNull(reopened.RootStorage.OpenStorage("Embedded").CreationTime);
    }

    /// <summary>
    /// Verifies that setting the creation time on a read-only file throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void CreationTime_WhenSetOnReadOnlyFile_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.RootStorage.CreationTime = DateTimeOffset.UnixEpoch;
        });
    }
}
