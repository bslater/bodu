// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Directory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Verifies that the unused slots of the last directory sector are written as free entries: all zeroes except the
    /// left sibling, right sibling and child links, which are NOSTREAM.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDirectoryHasUnusedEntries_ShouldWriteThemAsFreeEntries()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(100));
        _ = builder.AddStream("B", CompoundContainerBytes.Pattern(100));

        byte[] bytes = builder.ToArray();

        List<int> slots = CompoundContainerBytes.DirectoryEntryOffsets(bytes);
        Assert.HasCount(4, slots);
        byte[] expected = new byte[CompoundContainerBytes.DirectoryEntrySize];
        expected.AsSpan(CompoundContainerBytes.LeftSiblingOffset, 12).Fill(0xFF);
        CollectionAssert.AreEqual(expected, bytes.AsSpan(slots[3], CompoundContainerBytes.DirectoryEntrySize).ToArray());
    }

    /// <summary>
    /// Verifies that a storage's entry is written with a starting sector and a size of zero, as [MS-CFB] requires of a
    /// storage object.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenStorageIsWritten_ShouldZeroItsStartSectorAndSize()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStorage("S").AddStream("A", CompoundContainerBytes.Pattern(100));

        byte[] bytes = builder.ToArray();

        int entry = CompoundContainerBytes.FindEntryOffset(bytes, "S");
        CollectionAssert.AreEqual(new byte[12], bytes.AsSpan(entry + CompoundContainerBytes.StartSectorOffset, 12).ToArray());
    }

    /// <summary>
    /// Verifies that a stream's entry is written with creation and modified times of zero, as [MS-CFB] requires of a
    /// stream object, even when the stream's builder carries times, while a storage keeps its own.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenStreamIsWritten_ShouldZeroItsTimes()
    {
        var time = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var builder = CompoundStorageBuilder.CreateRoot();
        CompoundStorageBuilder storage = builder.AddStorage("S");
        storage.CreationTime = time;
        storage.ModifiedTime = time;
        CompoundStreamBuilder stream = builder.AddStream("A", CompoundContainerBytes.Pattern(100));
        stream.CreationTime = time;
        stream.ModifiedTime = time;

        byte[] bytes = builder.ToArray();

        int streamEntry = CompoundContainerBytes.FindEntryOffset(bytes, "A");
        int storageEntry = CompoundContainerBytes.FindEntryOffset(bytes, "S");
        CollectionAssert.AreEqual(new byte[16], bytes.AsSpan(streamEntry + CompoundContainerBytes.CreationTimeOffset, 16).ToArray());
        CollectionAssert.AreNotEqual(new byte[16], bytes.AsSpan(storageEntry + CompoundContainerBytes.CreationTimeOffset, 16).ToArray());
    }

    /// <summary>
    /// Verifies that every allocated directory entry is written black, the coloring [MS-CFB] names as the simplest
    /// valid one.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenTreeIsWritten_ShouldColorEveryEntryBlack()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        for (int i = 0; i < 7; i++)
            _ = builder.AddStream($"Stream{i}", CompoundContainerBytes.Pattern(100));

        CompoundStorageBuilder storage = builder.AddStorage("S");
        for (int i = 0; i < 3; i++)
            _ = storage.AddStream($"Nested{i}", CompoundContainerBytes.Pattern(100));

        byte[] bytes = builder.ToArray();

        int allocated = 0;
        foreach (int slot in CompoundContainerBytes.DirectoryEntryOffsets(bytes))
        {
            if (bytes[slot + CompoundContainerBytes.TypeOffset] == 0)
                continue;

            allocated++;
            Assert.AreEqual((byte)CompoundEntryColor.Black, bytes[slot + CompoundContainerBytes.ColorOffset]);
        }

        Assert.AreEqual(12, allocated);
    }
}
