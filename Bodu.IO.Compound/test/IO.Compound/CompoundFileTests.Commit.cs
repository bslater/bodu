// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Commit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

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
}
