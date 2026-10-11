// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.LargeContainers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies how the writer lays out containers of more than 2 GB without writing one.
/// </summary>
/// <remarks>
/// Each stream is deferred, with <see cref="StopWrite" /> as its opener, which throws instead of supplying content. The
/// writer emits the header and the directory before the content of the first regular stream, so a test either expects
/// the writer to reject the tree before it opens any stream, or catches the exception and inspects the directory
/// written so far, which records the size and the starting sector of every stream.
/// </remarks>
public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>The message of the exception a deferred stream's opener throws to stop the write.</summary>
    private const string StopWriteMessage = "The test stops the write before any stream content.";

    /// <summary>The number of 4096-byte sectors a 1,200,000,000-byte stream occupies.</summary>
    private const long SectorsOfLargeStream = 292969;

    /// <summary>
    /// The sector of a file of 4096-byte sectors that covers file offsets 0x7FFFFF00-0x7FFFFFFF, which [MS-CFB] 2.8
    /// reserves as the range lock sector.
    /// </summary>
    private const uint RangeLockSector = 0x7FFFE;

    /// <summary>
    /// Verifies that writing a version-3 container larger than the 2 GB [MS-CFB] allows a file of 512-byte sectors
    /// throws <see cref="CompoundFileSerializationException" /> before any stream is opened.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenVersion3ContainerWouldExceedTwoGigabytes_ShouldThrowCompoundFileSerializationException()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 1_200_000_000);
        _ = builder.AddStream("B", StopWrite, 1_200_000_000);
        var options = new CompoundBuildOptions { Version = CompoundFileVersion.V3 };

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            builder.WriteTo(Stream.Null, options);
        });
    }

    /// <summary>
    /// Verifies that every serializer of a version-3 tree whose container would exceed 2 GB throws
    /// <see cref="CompoundFileSerializationException" /> before it writes any byte or opens any stream.
    /// </summary>
    /// <param name="entryPoint">The serializer called.</param>
    [TestMethod]
    [DataRow("WriteToStream")]
    [DataRow("WriteToBufferWriter")]
    [DataRow("ToArray")]
    public void WriteTo_WhenVersion3ContainerWouldExceedTwoGigabytes_ShouldThrowBeforeWritingAnyByte(string entryPoint)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 1_100_000_000);
        _ = builder.AddStream("B", StopWrite, 1_100_000_000);
        var options = new CompoundBuildOptions { Version = CompoundFileVersion.V3 };
        using var destination = new MemoryStream();
        var output = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            switch (entryPoint)
            {
                case "WriteToStream":
                    builder.WriteTo(destination, options);
                    break;

                case "WriteToBufferWriter":
                    builder.WriteTo(output, options);
                    break;

                default:
                    _ = builder.ToArray(options);
                    break;
            }
        }, entryPoint);

        Assert.AreEqual(0, destination.Length, entryPoint);
        Assert.AreEqual(0, output.WrittenCount, entryPoint);
    }

    /// <summary>
    /// Verifies that a version-3 tree whose container fits in 2 GB is written: the writer reaches the content of its
    /// first stream.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenVersion3ContainerFitsInTwoGigabytes_ShouldStartWriting()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 1_000_000_000);
        _ = builder.AddStream("B", StopWrite, 1_000_000_000);
        using var destination = new MemoryStream();

        byte[] written = WriteUntilStopped(builder, destination, CompoundFileVersion.V3);

        Assert.AreEqual(3, BinaryPrimitives.ReadUInt16LittleEndian(written.AsSpan(0x1A)));
        Assert.AreEqual(1_000_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(written.AsSpan(CompoundContainerBytes.FindEntryOffset(written, "B") + CompoundContainerBytes.SizeOffset)));
    }

    /// <summary>
    /// Verifies that a version-4 container that passes 2 GB keeps the range lock sector (0x7FFFE) out of every chain: a
    /// stream whose sectors would cover it continues after it, so the next stream starts one sector later.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenVersion4ContainerExceedsTwoGigabytes_ShouldReserveRangeLockSector()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 1_200_000_000);
        _ = builder.AddStream("B", StopWrite, 1_200_000_000);
        _ = builder.AddStream("C", StopWrite, 4096);
        using var destination = new MemoryStream();

        byte[] written = WriteUntilStopped(builder, destination, CompoundFileVersion.V4);

        uint a = StartSector(written, "A");
        uint b = StartSector(written, "B");
        uint c = StartSector(written, "C");
        Assert.AreEqual(1u, a);
        Assert.AreEqual(1 + SectorsOfLargeStream, b);
        Assert.IsLessThan(b + SectorsOfLargeStream, 0x7FFFEL);
        Assert.AreEqual(b + SectorsOfLargeStream + 1, c);
    }

    /// <summary>
    /// Verifies that a version-4 container whose FAT would take the range lock sector (0x7FFFE) places that FAT sector,
    /// and every sector after it, one sector later, so the header's DIFAT array and first DIFAT sector skip it.
    /// </summary>
    /// <remarks>
    /// The stream fills sectors 1 to 524,280 and the FAT's 513 sectors follow it, so its sixth sector would be the
    /// range lock sector.
    /// </remarks>
    [TestMethod]
    public void WriteTo_WhenVersion4FatWouldTakeRangeLockSector_ShouldPlaceItAfterTheSector()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 524_280L * 4096);
        using var destination = new MemoryStream();

        byte[] written = WriteUntilStopped(builder, destination, CompoundFileVersion.V4);

        var header = CfbHeader.Parse(written);
        List<uint> fatSectors = CompoundContainerBytes.FatSectorIds(written);
        Assert.AreEqual(513u, header.FatSectorCount);
        Assert.AreEqual(RangeLockSector - 1, fatSectors[4]);
        Assert.AreEqual(RangeLockSector + 1, fatSectors[5]);
        CollectionAssert.DoesNotContain(fatSectors, RangeLockSector);
        Assert.AreEqual(524_795u, header.FirstDifatSector);
    }

    /// <summary>
    /// Verifies that a version-4 container whose last sector is the one before the range lock sector (0x7FFFE) is
    /// written without reserving it: the file ends before the sector, so its layout is unchanged.
    /// </summary>
    /// <remarks>
    /// The stream fills sectors 1 to 523,772, the FAT's 512 sectors follow it, and the DIFAT sector is sector 0x7FFFD.
    /// </remarks>
    [TestMethod]
    public void WriteTo_WhenVersion4ContainerEndsBeforeRangeLockSector_ShouldNotReserveIt()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", StopWrite, 523_772L * 4096);
        using var destination = new MemoryStream();

        byte[] written = WriteUntilStopped(builder, destination, CompoundFileVersion.V4);

        var header = CfbHeader.Parse(written);
        List<uint> fatSectors = CompoundContainerBytes.FatSectorIds(written);
        Assert.AreEqual(512u, header.FatSectorCount);
        for (int i = 0; i < fatSectors.Count; i++)
            Assert.AreEqual(523_773u + (uint)i, fatSectors[i]);

        Assert.AreEqual(RangeLockSector - 1, header.FirstDifatSector);
    }

    /// <summary>
    /// Verifies that a version-4 stream of more than 2 GB is written with its full 64-bit size.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenVersion4StreamExceedsTwoGigabytes_ShouldRecordItsSize()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Big", StopWrite, 3_000_000_000);
        using var destination = new MemoryStream();

        byte[] written = WriteUntilStopped(builder, destination, CompoundFileVersion.V4);

        int entry = CompoundContainerBytes.FindEntryOffset(written, "Big");
        Assert.AreEqual(3_000_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(written.AsSpan(entry + CompoundContainerBytes.SizeOffset)));
        Assert.AreEqual(1u, StartSector(written, "Big"));
    }

    /// <summary>
    /// Stands in for the opener of a deferred stream, stopping the write instead of supplying content.
    /// </summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="InvalidOperationException">Always.</exception>
    private static Stream StopWrite() =>
        throw new InvalidOperationException(StopWriteMessage);

    /// <summary>
    /// Writes a tree whose streams are opened by <see cref="StopWrite" /> and returns what was written before the first
    /// stream was opened.
    /// </summary>
    /// <param name="builder">The tree to write.</param>
    /// <param name="destination">The stream the writer writes to.</param>
    /// <param name="version">The version to write.</param>
    /// <returns>The bytes written before the write stopped.</returns>
    private static byte[] WriteUntilStopped(CompoundStorageBuilder builder, MemoryStream destination, CompoundFileVersion version)
    {
        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            builder.WriteTo(destination, new CompoundBuildOptions { Version = version });
        });

        Assert.AreEqual(StopWriteMessage, ex.Message);
        return destination.ToArray();
    }

    /// <summary>
    /// Returns the starting sector a directory entry records.
    /// </summary>
    /// <param name="container">The bytes written.</param>
    /// <param name="name">The name of the entry.</param>
    /// <returns>The entry's starting sector.</returns>
    private static uint StartSector(byte[] container, string name) =>
        BinaryPrimitives.ReadUInt32LittleEndian(
            container.AsSpan(CompoundContainerBytes.FindEntryOffset(container, name) + CompoundContainerBytes.StartSectorOffset));
}
