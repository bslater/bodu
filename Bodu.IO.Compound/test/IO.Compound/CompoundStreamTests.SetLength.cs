// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.SetLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that setting a length beyond <see cref="int.MaxValue" /> throws
    /// <see cref="NotSupportedException" /> before allocating.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenBeyondInt32MaxValue_ShouldThrowNotSupportedException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        using CompoundStream cursor = file.RootStorage.CreateStream("Data");

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            cursor.SetLength(int.MaxValue + 1L);
        });
    }

    /// <summary>
    /// Verifies that a truncating <see cref="CompoundStream.SetLength(long)" /> alone marks a pending change that
    /// the dispose-time flush persists.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenShrinkingExistingPayload_ShouldPersistTruncationOnDispose()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3, 4 });
        using (CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
            cursor.SetLength(2);

        file.Commit();

        using var reopened = CompoundFile.Open(new MemoryStream(destination.ToArray(), writable: false));
        using CompoundStream read = reopened.RootStorage.OpenStream("Data");
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, read.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that changing the length of a cursor keeps its position unless the new length cuts the stream short
    /// before it.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenGrowing_ShouldKeepThePosition()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.CreateStream("Data");
        cursor.Write(CompoundContainerBytes.Pattern(10000));
        cursor.Seek(6000, SeekOrigin.Begin);

        cursor.SetLength(7000);
        Assert.AreEqual(6000, cursor.Position);

        cursor.SetLength(20000);
        Assert.AreEqual(6000, cursor.Position);

        cursor.SetLength(5000);
        Assert.IsLessThanOrEqualTo(5000L, cursor.Position);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.SetLength(long)" /> on a writable cursor throws
    /// <see cref="ObjectDisposedException" /> once the file that owns the cursor has been disposed, and leaves the
    /// cursor's length as it was.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenOwningFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cursor.SetLength(1);
        });

        Assert.AreEqual(3L, cursor.Length);
    }

    /// <summary>
    /// Verifies that a committed stream shrunk through the writable cursor between two lengths that sit either side
    /// of, at, or past the mini-stream cutoff, or to empty, and committed again, is committed intact: the stream
    /// moves from the regular sectors to the mini stream, or out of both to empty, where the lengths straddle the
    /// cutoff, and stays in the regular sectors where both are at or above it; a strict reopen accepts the
    /// container, reads back exactly the bytes kept, and finds the stream beside it unchanged.
    /// </summary>
    /// <param name="before">The length of the stream as first committed.</param>
    /// <param name="after">The length set through the cursor before the second commit.</param>
    [TestMethod]
    [DataRow(MiniStreamCutoff + 1, MiniStreamCutoff)]
    [DataRow(MiniStreamCutoff, MiniStreamCutoff - 1)]
    [DataRow(2 * MiniStreamCutoff, MiniStreamCutoff - 1)]
    [DataRow(MiniStreamCutoff, 0)]
    public void SetLength_WhenShrinkingAcrossTheMiniStreamCutoff_ShouldCommitAndReadBackIntact(int before, int after)
    {
        using MemoryStream destination = CreateCommittedContainer(before);

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            using (CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
                cursor.SetLength(after);

            file.Commit();
        }

        AssertCommittedStreams(destination.ToArray(), CompoundContainerBytes.Pattern(after));
    }

    /// <summary>
    /// Verifies that a committed stream a write grows across the mini-stream cutoff and a shorter length then brings
    /// back below it, both in one edit session before its single commit, is committed at the shorter length with the
    /// written bytes it still holds: a strict reopen accepts the container, reads back exactly those bytes, and finds
    /// the stream beside it unchanged.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenShrinkingBackBelowTheMiniStreamCutoffAfterAWriteGrewAcrossIt_ShouldCommitAndReadBackIntact()
    {
        const int Written = 2 * MiniStreamCutoff;
        const int Kept = 1000;
        using MemoryStream destination = CreateCommittedContainer(4000);
        byte[] edited = CompoundContainerBytes.Pattern(Written, EditedPatternStart);

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            using (CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
            {
                cursor.Write(edited);
                Assert.AreEqual((long)Written, cursor.Length);

                cursor.SetLength(Kept);
                Assert.AreEqual((long)Kept, cursor.Length);
            }

            file.Commit();
        }

        AssertCommittedStreams(destination.ToArray(), edited[..Kept]);
    }
}
