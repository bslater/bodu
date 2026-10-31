// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Commit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Builders;
using Bodu.Test.IO;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that a commit that leaves the container shorter than its destination truncates the destination to the
    /// committed container, so that nothing remains past its last sector.
    /// </summary>
    [TestMethod]
    public void Commit_WhenContentShrinks_ShouldTruncateDestination()
    {
        MemoryStream destination = CreateContainer(
            ("Large", CompoundContainerBytes.Pattern(50000)),
            ("Small", CompoundContainerBytes.Pattern(100)));
        long originalLength = destination.Length;

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            Assert.IsTrue(file.RootStorage.Delete("Large"));
            file.Commit();
        }

        byte[] committed = destination.ToArray();
        Assert.IsLessThan(originalLength, committed.LongLength);
        Assert.AreEqual(CompoundContainerBytes.AllocatedLength(committed), committed.LongLength);
    }

    /// <summary>
    /// Verifies that a commit after a stream is deleted leaves neither the stream's name nor its content anywhere in the
    /// committed container.
    /// </summary>
    [TestMethod]
    public void Commit_WhenEntryDeleted_ShouldLeaveNoTraceOfItsNameOrContent()
    {
        byte[] deleted = new byte[5000];
        deleted.AsSpan().Fill(0xA5);
        byte[] kept = new byte[100];
        kept.AsSpan().Fill(0x11);
        MemoryStream destination = CreateContainer(("LongStreamName", deleted), ("B", kept));

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            Assert.IsTrue(file.RootStorage.Delete("LongStreamName"));
            file.Commit();
        }

        byte[] committed = destination.ToArray();
        byte[] run = new byte[64];
        run.AsSpan().Fill(0xA5);
        Assert.AreEqual(-1, committed.AsSpan().IndexOf(Encoding.Unicode.GetBytes("LongStreamName")));
        Assert.AreEqual(-1, committed.AsSpan().IndexOf(run));
    }

    /// <summary>
    /// Verifies that a commit in update mode keeps the class identifier that the source container records for its root.
    /// </summary>
    [TestMethod]
    public void Commit_WhenRootHasClassId_ShouldPreserveIt()
    {
        var classId = new Guid("00020906-0000-0000-C000-000000000046");
        var destination = new MemoryStream();
        using (var seed = CompoundFile.Create(destination, leaveOpen: true))
        {
            seed.RootStorage.ClassId = classId;
            seed.RootStorage.CreateStream("A", CompoundContainerBytes.Pattern(100));
            seed.Commit();
        }

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            file.RootStorage.CreateStream("B", CompoundContainerBytes.Pattern(10));
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);
        Assert.AreEqual(classId, reopened.RootStorage.ClassId);
        Assert.IsTrue(reopened.RootStorage.TryOpenStream("B", out _));
    }

    /// <summary>
    /// Verifies that a commit writes a transaction signature of zero, as [MS-CFB] requires of an implementation without
    /// file transactions, whatever signature the source container recorded.
    /// </summary>
    [TestMethod]
    public void Commit_WhenSourceRecordsTransactionSignature_ShouldWriteZero()
    {
        byte[] bytes = CreateContainer(("A", CompoundContainerBytes.Pattern(100))).ToArray();
        CompoundContainerBytes.WriteUInt32(bytes, 0x34, 5);
        var destination = new MemoryStream();
        destination.Write(bytes);

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
            file.Commit();

        Assert.AreEqual(0u, BinaryPrimitives.ReadUInt32LittleEndian(destination.ToArray().AsSpan(0x34)));
    }

    /// <summary>
    /// Verifies that a commit after a stream is shrunk or removed leaves only the bytes the stream still holds, with no
    /// stale copy of its former content in the unused part of a sector.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="length">The length of the stream as first committed.</param>
    /// <param name="remaining">The length of the stream after the edit, or 0 when it is removed.</param>
    /// <param name="remove">Whether the edit removes the stream rather than shrinking it.</param>
    [TestMethod]
    [DataRow("RegularStreamShrunk", 5000, 4100, false)]
    [DataRow("MiniStreamShrunk", 200, 100, false)]
    [DataRow("MiniStreamRemoved", 200, 0, true)]
    public void Commit_WhenStreamShrinksOrIsRemoved_ShouldLeaveNoStaleBytes(string testName, int length, int remaining, bool remove)
    {
        byte[] content = new byte[length];
        content.AsSpan().Fill(0xAB);
        MemoryStream destination = CreateContainer(("Data", content), ("Other", new byte[10]));

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            if (remove)
            {
                Assert.IsTrue(file.RootStorage.Delete("Data"));
            }
            else
            {
                using CompoundStream stream = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite);
                stream.SetLength(remaining);
            }

            file.Commit();
        }

        byte[] committed = destination.ToArray();
        Assert.AreEqual(remaining, committed.Count(static value => value == 0xAB), testName);
    }

    /// <summary>
    /// Verifies that a commit of a created version-3 file throws <see cref="CompoundFileSerializationException" /> when
    /// the container would exceed the 2 GB that [MS-CFB] allows a file of 512-byte sectors.
    /// </summary>
    [TestMethod]
    public void Commit_WhenCreatedContainerWouldExceedTwoGigabytes_ShouldThrowCompoundFileSerializationException()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using var first = new UnbackedMemoryManager(1_100_000_000);
        using var second = new UnbackedMemoryManager(1_100_000_000);
        file.RootStorage.CreateStream("Large1", first.Memory);
        file.RootStorage.CreateStream("Large2", second.Memory);

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            file.Commit();
        });
    }

    /// <summary>
    /// Verifies that a commit in update mode, which always writes version 3, throws
    /// <see cref="CompoundFileSerializationException" /> when the container would exceed the 2 GB that [MS-CFB] allows a
    /// file of 512-byte sectors.
    /// </summary>
    /// <remarks>
    /// The two streams are <see cref="UnbackedMemoryManager" /> payloads, which report their length without holding
    /// their bytes, so the test allocates nothing large: a writer that checks the size first throws the expected
    /// exception, and one that starts to copy the payloads fails at once.
    /// </remarks>
    [TestMethod]
    public void Commit_WhenUpdatedContainerWouldExceedTwoGigabytes_ShouldThrowCompoundFileSerializationException()
    {
        MemoryStream destination = CreateContainer(("A", CompoundContainerBytes.Pattern(100)));
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        using var first = new UnbackedMemoryManager(1_100_000_000);
        using var second = new UnbackedMemoryManager(1_100_000_000);
        file.RootStorage.CreateStream("Large1", first.Memory);
        file.RootStorage.CreateStream("Large2", second.Memory);

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            file.Commit();
        });
    }

    /// <summary>
    /// Verifies that the directory slots a commit leaves unused are written as free entries, all zeroes except the left
    /// sibling, right sibling and child links, which are NOSTREAM, for a created file and for one committed again after
    /// an edit, with 512-byte and 4096-byte sectors.
    /// </summary>
    /// <param name="version">The version the file is created with.</param>
    [TestMethod]
    [DataRow(CompoundFileVersion.V3)]
    [DataRow(CompoundFileVersion.V4)]
    public void Commit_WhenDirectoryHasUnusedSlots_ShouldWriteThemAsFreeEntries(CompoundFileVersion version)
    {
        var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true, new CompoundBuildOptions { Version = version });
        file.RootStorage.CreateStream("A", CompoundContainerBytes.Pattern(100));

        file.Commit();
        AssertFreeEntries(destination.ToArray(), expectedFree: version == CompoundFileVersion.V3 ? 2 : 30);

        file.RootStorage.CreateStream("B", CompoundContainerBytes.Pattern(100));
        file.Commit();
        AssertFreeEntries(destination.ToArray(), expectedFree: version == CompoundFileVersion.V3 ? 1 : 29);
    }

    /// <summary>
    /// Verifies that a commit in update mode writes the directory slots it leaves unused as free entries.
    /// </summary>
    [TestMethod]
    public void Commit_WhenUpdatedDirectoryHasUnusedSlots_ShouldWriteThemAsFreeEntries()
    {
        MemoryStream destination = CreateContainer(("A", CompoundContainerBytes.Pattern(100)), ("B", CompoundContainerBytes.Pattern(100)));

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            Assert.IsTrue(file.RootStorage.Delete("B"));
            file.Commit();
        }

        AssertFreeEntries(destination.ToArray(), expectedFree: 2);
    }

    /// <summary>
    /// Asserts that every unallocated slot of a container's directory is a free entry as [MS-CFB] 2.6.3 defines it.
    /// </summary>
    /// <param name="container">The committed container.</param>
    /// <param name="expectedFree">The number of unallocated slots the directory is expected to hold.</param>
    private static void AssertFreeEntries(byte[] container, int expectedFree)
    {
        byte[] free = new byte[CompoundContainerBytes.DirectoryEntrySize];
        free.AsSpan(CompoundContainerBytes.LeftSiblingOffset, 12).Fill(0xFF);
        int found = 0;
        foreach (int slot in CompoundContainerBytes.DirectoryEntryOffsets(container))
        {
            if (container[slot + CompoundContainerBytes.TypeOffset] != 0)
                continue;

            found++;
            CollectionAssert.AreEqual(free, container.AsSpan(slot, free.Length).ToArray(), $"slot at {slot}");
        }

        Assert.AreEqual(expectedFree, found);
    }

    /// <summary>
    /// Verifies that a commit in update mode whose staged tree cannot be written, its storages nested deeper than
    /// <see cref="CompoundBuildOptions.MaxDepth" /> allows, throws <see cref="CompoundFileSerializationException" /> and
    /// leaves the container it opened unchanged.
    /// </summary>
    [TestMethod]
    public void Commit_WhenStagedTreeCannotBeWritten_ShouldLeaveTheDestinationUnchanged()
    {
        MemoryStream destination = CreateContainer(("A", CompoundContainerBytes.Pattern(100)));
        byte[] original = destination.ToArray();

        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        CompoundStorage storage = file.RootStorage;
        for (int depth = 0; depth < 70; depth++)
            storage = storage.CreateStorage("S");

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            file.Commit();
        });

        CollectionAssert.AreEqual(original, destination.ToArray());
    }

    /// <summary>
    /// Verifies that a second commit of a created version-3 file whose container would exceed 2 GB throws
    /// <see cref="CompoundFileSerializationException" /> and leaves the container the first commit wrote unchanged.
    /// </summary>
    [TestMethod]
    public void Commit_WhenSecondCommitWouldExceedTwoGigabytes_ShouldLeaveTheFirstCommitUnchanged()
    {
        var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        file.RootStorage.CreateStream("A", CompoundContainerBytes.Pattern(100));
        file.Commit();
        byte[] first = destination.ToArray();
        using var large1 = new UnbackedMemoryManager(1_100_000_000);
        using var large2 = new UnbackedMemoryManager(1_100_000_000);
        file.RootStorage.CreateStream("Large1", large1.Memory);
        file.RootStorage.CreateStream("Large2", large2.Memory);

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            file.Commit();
        });

        CollectionAssert.AreEqual(first, destination.ToArray());
    }

    /// <summary>
    /// Verifies that a commit in update mode whose staged tree cannot be written leaves the file dirty, and that a
    /// <see cref="CompoundFile.Revert" /> followed by a commit succeeds and writes the tree that was loaded.
    /// </summary>
    [TestMethod]
    public void Commit_WhenStagedTreeCannotBeWritten_ShouldStayDirtyAndCommitAfterRevert()
    {
        using var destination = new MemoryStream();
        destination.Write(CreateSeedContainer());
        destination.Position = 0;
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        CompoundStorage storage = file.RootStorage;
        for (int depth = 0; depth < 70; depth++)
            storage = storage.CreateStorage("S");

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            file.Commit();
        });

        Assert.IsTrue(file.IsDirty);

        file.Revert();
        file.Commit();

        Assert.IsFalse(file.IsDirty);
        AssertHoldsSeedContainer(destination.ToArray());
    }

    /// <summary>
    /// Verifies that a commit in update mode whose destination faults part way through the write throws the
    /// destination's <see cref="IOException" /> unwrapped, leaves the file dirty, and leaves the destination partially
    /// written: shorter than the container, longer than nothing, and not a container the reader accepts.
    /// </summary>
    [TestMethod]
    public void Commit_WhenDestinationFaultsMidWrite_ShouldLeaveDestinationPartiallyWrittenAndFileDirty()
    {
        byte[] seed = CreateSeedContainer();
        byte[] full = CommitSampleEditsToMemory(seed);
        using var destination = new FaultingWriteStream(seed, MidWriteFaultBytes);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);

        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        Assert.IsTrue(file.IsDirty);
        AssertPartiallyWritten(destination.ToArray(), full);
    }

    /// <summary>
    /// Verifies that a commit retried over a destination that faulted part way through the previous write produces the
    /// bytes a fault-free commit of the same tree produces, clears the dirty state, and reopens with the staged entries.
    /// </summary>
    [TestMethod]
    public void Commit_WhenRetriedAfterMidWriteFault_ShouldWriteWholeContainer()
    {
        byte[] seed = CreateSeedContainer();
        byte[] full = CommitSampleEditsToMemory(seed);
        using var destination = new FaultingWriteStream(seed, MidWriteFaultBytes);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);
        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        destination.Disarm();
        file.Commit();

        Assert.IsFalse(file.IsDirty);
        CollectionAssert.AreEqual(full, destination.ToArray());
        AssertHoldsSampleEdits(destination.ToArray());
    }

    /// <summary>
    /// Verifies that a commit whose destination throws from its flush propagates the <see cref="IOException" /> after
    /// every sector has been written and leaves the file dirty, and that a retry once the destination flushes again
    /// writes the whole container a second time, flushes it, and clears the dirty state.
    /// </summary>
    [TestMethod]
    public void Commit_WhenDestinationFlushThrows_ShouldPropagateAndStayDirtyUntilRetried()
    {
        byte[] seed = CreateSeedContainer();
        byte[] full = CommitSampleEditsToMemory(seed);
        using var destination = new FaultingWriteStream(seed, long.MaxValue, throwOnFlush: true);
        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);

        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        Assert.IsTrue(file.IsDirty);
        Assert.AreEqual(1, destination.FlushCount);
        Assert.AreEqual(full.LongLength, destination.BytesWritten);

        destination.Disarm();
        file.Commit();

        Assert.IsFalse(file.IsDirty);
        Assert.AreEqual(2, destination.FlushCount);
        Assert.AreEqual(2 * full.LongLength, destination.BytesWritten);
        CollectionAssert.AreEqual(full, destination.ToArray());
    }

    /// <summary>
    /// Verifies that a commit to a seekable destination whose position is not at the start writes the container from
    /// offset zero and truncates whatever the destination held, so the destination is exactly the container.
    /// </summary>
    [TestMethod]
    public void Commit_WhenDestinationPositionIsNotZero_ShouldWriteFromStart()
    {
        byte[] expected;
        using (var plain = new MemoryStream())
        {
            using CompoundFile reference = CreateSampleTree(plain);
            reference.Commit();
            expected = plain.ToArray();
        }

        using var destination = new MemoryStream();
        destination.Write(CompoundContainerBytes.Pattern(777));
        using (CompoundFile file = CreateSampleTree(destination))
        {
            destination.Position = 300;
            file.Commit();
        }

        Assert.AreEqual(expected.LongLength, destination.Length);
        CollectionAssert.AreEqual(expected, destination.ToArray());
    }

    /// <summary>
    /// Verifies that a commit to a non-seekable destination that faults part way through the write leaves the bytes
    /// written so far in place, and that a retry appends one whole container after them, as the documented
    /// append-at-current-position behavior of a non-seekable destination requires.
    /// </summary>
    [TestMethod]
    public void Commit_WhenNonSeekableDestinationFaultsMidWrite_ShouldAppendRetryAfterPartialBytes()
    {
        byte[] full;
        using (var plain = new MemoryStream())
        {
            using CompoundFile reference = CreateSampleTree(plain);
            reference.Commit();
            full = plain.ToArray();
        }

        using var sink = new FaultingWriteStream(MidWriteFaultBytes, seekable: false);
        using CompoundFile file = CreateSampleTree(sink);
        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        var partial = (int)sink.BytesWritten;
        Assert.IsTrue(file.IsDirty);
        Assert.IsGreaterThan(0, partial);
        Assert.IsLessThan(full.Length, partial);

        sink.Disarm();
        file.Commit();

        byte[] written = sink.ToArray();
        Assert.IsFalse(file.IsDirty);
        Assert.AreEqual(partial + full.Length, written.Length);
        CollectionAssert.AreEqual(full.AsSpan(0, partial).ToArray(), written.AsSpan(0, partial).ToArray());
        CollectionAssert.AreEqual(full, written.AsSpan(partial).ToArray());
    }
}
