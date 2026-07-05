// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbDirectoryHardeningTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that the directory parser walks a deeply nested red-black sibling tree iteratively so a crafted directory
/// with a degenerate spine is handled without overflowing the call stack.
/// </summary>
[TestClass]
public class CfbDirectoryHardeningTests
{
    /// <summary>The fixed size, in bytes, of a directory entry.</summary>
    private const int DirectoryEntrySize = 128;

    /// <summary>The end-of-chain / no-stream sentinel for sibling and child links.</summary>
    private const uint NoStream = 0xFFFFFFFF;

    /// <summary>
    /// Verifies that a directory whose child red-black tree forms a left spine thousands of entries deep is parsed
    /// successfully, proving the walk is iterative and does not exhaust the call stack.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Constructor_WhenChildTreeSpineIsDeep_ShouldParseWithoutStackOverflow()
    {
        // 20,000 chained left-siblings would overflow a recursive in-order walk; the iterative walk handles it.
        const int depth = 20_000;
        byte[] directory = BuildLeftSpineDirectory(depth);
        CfbHeader header = ParseReferenceHeader();

        var parsed = new CfbDirectory(directory, header, CompoundValidationLevel.Compatible);

        Assert.AreEqual(depth, parsed.Root.Children.Count);
    }

    /// <summary>
    /// Builds directory bytes with a root storage whose child tree is a left spine of <paramref name="depth" />
    /// stream entries (SID 1 → 2 → … via the left-sibling link).
    /// </summary>
    /// <param name="depth">The number of chained child entries.</param>
    /// <returns>The serialized directory bytes.</returns>
    private static byte[] BuildLeftSpineDirectory(int depth)
    {
        int count = depth + 1;
        byte[] directory = new byte[count * DirectoryEntrySize];

        // SID 0: the root storage, whose child link anchors the spine at SID 1.
        WriteEntry(directory, sid: 0, name: "Root", type: CompoundEntryType.RootStorage, left: NoStream, right: NoStream, child: depth >= 1 ? 1u : NoStream);

        for (int sid = 1; sid <= depth; sid++)
        {
            uint left = sid < depth ? (uint)(sid + 1) : NoStream;
            WriteEntry(directory, sid, name: "S" + sid.ToString(System.Globalization.CultureInfo.InvariantCulture), type: CompoundEntryType.Stream, left: left, right: NoStream, child: NoStream);
        }

        return directory;
    }

    /// <summary>
    /// Writes a single directory entry at the specified stream identifier.
    /// </summary>
    /// <param name="directory">The directory buffer.</param>
    /// <param name="sid">The stream identifier (entry index).</param>
    /// <param name="name">The entry name.</param>
    /// <param name="type">The entry type.</param>
    /// <param name="left">The left-sibling link.</param>
    /// <param name="right">The right-sibling link.</param>
    /// <param name="child">The child link.</param>
    private static void WriteEntry(byte[] directory, int sid, string name, CompoundEntryType type, uint left, uint right, uint child)
    {
        Span<byte> entry = directory.AsSpan(sid * DirectoryEntrySize, DirectoryEntrySize);
        int nameBytes = Encoding.Unicode.GetBytes(name, entry.Slice(0, 62));
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(64), (ushort)(nameBytes + 2));
        entry[66] = (byte)type;
        entry[67] = 1; // black
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(68), left);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(72), right);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(76), child);
    }

    /// <summary>
    /// Parses the header of a minimal valid container to obtain realistic sector-size metadata.
    /// </summary>
    /// <returns>The parsed <see cref="CfbHeader" />.</returns>
    private static CfbHeader ParseReferenceHeader()
    {
        byte[] container = CompoundStorageBuilder.CreateRoot().ToArray();
        return CfbHeader.Parse(container);
    }
}
