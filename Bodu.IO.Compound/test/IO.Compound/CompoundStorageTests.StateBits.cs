// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.StateBits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that state bits set on the root and a child storage persist across commit and reopen, visible
    /// through the property, <see cref="CompoundStorage.Stat" />, and the parent's entry enumeration.
    /// </summary>
    [TestMethod]
    public void StateBits_WhenSetOnWritableStorage_ShouldPersistAcrossCommitAndReopen()
    {
        const int rootBits = 0x0000_2A00;
        const int childBits = unchecked((int)0x8000_0001);
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.StateBits = rootBits;
            CompoundStorage child = file.RootStorage.CreateStorage("Embedded");
            child.StateBits = childBits;
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.AreEqual(rootBits, reopened.RootStorage.StateBits);
        Assert.AreEqual(rootBits, reopened.RootStorage.Stat.StateBits);

        CompoundStorage storage = reopened.RootStorage.OpenStorage("Embedded");
        Assert.AreEqual(childBits, storage.StateBits);
        Assert.AreEqual(childBits, reopened.RootStorage.EnumerateEntries().Single(e => e.Name == "Embedded").StateBits);
    }

    /// <summary>
    /// Verifies that a storage created through the edit surface carries zero state bits unless set.
    /// </summary>
    [TestMethod]
    public void StateBits_WhenNeverSet_ShouldRemainZeroAfterCommit()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStorage("Embedded");
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);

        Assert.AreEqual(0, reopened.RootStorage.OpenStorage("Embedded").StateBits);
    }

    /// <summary>
    /// Verifies that setting the state bits on a read-only file throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void StateBits_WhenSetOnReadOnlyFile_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            file.RootStorage.StateBits = 1;
        });
    }
}
