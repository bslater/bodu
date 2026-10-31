// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Revert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundFile.Revert" /> after a commit in update mode faulted part way through the
    /// write restores the tree that was loaded and clears the dirty state, so that the next commit writes the original
    /// entries and bytes over the partially written destination.
    /// </summary>
    [TestMethod]
    public void Revert_WhenCommitFaultedMidWrite_ShouldRestoreLoadedTreeOnNextCommit()
    {
        using var destination = new FaultingWriteStream(CreateSeedContainer(), MidWriteFaultBytes);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);
        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        file.Revert();

        Assert.IsFalse(file.IsDirty);
        Assert.IsTrue(file.RootStorage.TryOpenStream("A", out _));
        Assert.IsFalse(file.RootStorage.TryOpenStream("Meta", out _));

        destination.Disarm();
        file.Commit();

        Assert.IsFalse(file.IsDirty);
        AssertHoldsSeedContainer(destination.ToArray());
    }
}
