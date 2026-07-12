// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.ModifiedTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that a modification time set on the root and a child storage persists across commit and reopen,
    /// surfacing through the property and the <see cref="CompoundEntryInfo.LastModifiedTime" /> snapshot.
    /// </summary>
    [TestMethod]
    public void ModifiedTime_WhenSetOnWritableStorage_ShouldPersistAcrossCommitAndReopen()
    {
        var rootTime = new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var childTime = new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero);
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.ModifiedTime = rootTime;
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.ModifiedTime = childTime;
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.AreEqual(rootTime, reopened.RootStorage.ModifiedTime);
        Assert.AreEqual(rootTime, reopened.RootStorage.Stat.LastModifiedTime);

        CompoundStorage storage = reopened.RootStorage.OpenStorage("Embedded");
        Assert.AreEqual(childTime, storage.ModifiedTime);
        Assert.AreEqual(childTime, reopened.RootStorage.EnumerateEntries().Single(e => e.Name == "Embedded").LastModifiedTime);
    }

    /// <summary>
    /// Verifies that a modification time is never stamped automatically: a storage committed without setting one
    /// reads back <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ModifiedTime_WhenNeverSet_ShouldRemainNullAfterCommit()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.CreateStream("Data", new byte[] { 1, 2, 3 });
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.IsNull(reopened.RootStorage.OpenStorage("Embedded").ModifiedTime);
    }

    /// <summary>
    /// Verifies that setting the modification time on a read-only file throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ModifiedTime_WhenSetOnReadOnlyFile_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.RootStorage.ModifiedTime = DateTimeOffset.UnixEpoch;
        });
    }
}
