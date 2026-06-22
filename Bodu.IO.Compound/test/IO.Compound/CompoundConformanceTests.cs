// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundConformanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies MS-CFB conformance rules enforced when writing a compound file: stream objects carry no CLSID or time
/// metadata, storage objects preserve theirs, and version-3 stream sizes are bounded.
/// </summary>
[TestClass]
public class CompoundConformanceTests
{
    /// <summary>The byte offset, within a directory entry, of the 16-bit UTF-16 name length.</summary>
    private const int NameLengthOffset = 64;

    /// <summary>The byte offset, within a directory entry, of the 16-byte CLSID.</summary>
    private const int ClassIdOffset = 80;

    /// <summary>The byte offset, within a directory entry, of the 64-bit creation FILETIME.</summary>
    private const int CreationTimeOffset = 100;

    /// <summary>The byte offset, within a directory entry, of the 64-bit modified FILETIME.</summary>
    private const int ModifiedTimeOffset = 108;

    /// <summary>The fixed size, in bytes, of a directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>
    /// Verifies that a stream object's CLSID and creation/modified times are written as zero even when the builder node
    /// carries metadata, and that a storage object preserves its CLSID.
    /// </summary>
    [TestMethod]
    public void Write_WhenStreamAndStorageHaveMetadata_ShouldZeroStreamButPreserveStorage()
    {
        var storageClassId = new Guid("11112222-3333-4444-5555-666677778888");

        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Data", new byte[] { 1, 2, 3 });
        stream.ClassId = new Guid("99990000-1111-2222-3333-444455556666");
        stream.CreationTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        stream.ModifiedTime = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        CompoundStorageBuilder storage = root.AddStorage("Store");
        storage.ClassId = storageClassId;

        byte[] bytes = root.ToArray();
        CfbHeader header = CfbHeader.Parse(bytes);

        int streamEntry = FindEntryOffset(bytes, header, "Data");
        Assert.AreEqual(Guid.Empty, new Guid(bytes.AsSpan(streamEntry + ClassIdOffset, 16)), "stream clsid");
        Assert.AreEqual(0, BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(streamEntry + CreationTimeOffset)), "stream creation time");
        Assert.AreEqual(0, BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(streamEntry + ModifiedTimeOffset)), "stream modified time");

        int storageEntry = FindEntryOffset(bytes, header, "Store");
        Assert.AreEqual(storageClassId, new Guid(bytes.AsSpan(storageEntry + ClassIdOffset, 16)), "storage clsid");
    }

    /// <summary>
    /// Verifies that writing a version-3 file rejects a stream declared larger than the 2 GB version-3 bound.
    /// </summary>
    [TestMethod]
    public void Write_WhenVersion3StreamExceeds2GB_ShouldThrowSerializationException()
    {
        var root = CompoundStorageBuilder.CreateRoot();

        // A deferred source with a declared length beyond 0x80000000; the guard runs before the source is read.
        _ = root.AddStream("Huge", () => new MemoryStream(), 0x80000001);

        Assert.ThrowsExactly<CompoundFileSerializationException>(
            () => _ = root.ToArray(new CompoundBuildOptions { Version = CompoundFileVersion.V3 }));
    }

    /// <summary>
    /// Locates the byte offset of the directory entry with the specified name within the first directory sector.
    /// </summary>
    /// <param name="bytes">The compound-file bytes.</param>
    /// <param name="header">The parsed header.</param>
    /// <param name="name">The entry name to find.</param>
    /// <returns>The zero-based byte offset of the matching directory entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entry is not found.</exception>
    private static int FindEntryOffset(byte[] bytes, CfbHeader header, string name)
    {
        int dirOffset = (int)(((long)header.FirstDirectorySector + 1) * header.SectorSize);
        int perSector = header.SectorSize / DirectoryEntrySize;

        for (int i = 0; i < perSector; i++)
        {
            int entryOffset = dirOffset + (i * DirectoryEntrySize);
            ushort nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(entryOffset + NameLengthOffset));
            if (nameLength is < 4 or > 64)
                continue;

            string entryName = Encoding.Unicode.GetString(bytes, entryOffset, nameLength - 2);
            if (entryName == name)
                return entryOffset;
        }

        throw new InvalidOperationException($"Directory entry '{name}' was not found.");
    }
}
