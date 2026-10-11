// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.Layout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetWriterTests
{
    /// <summary>The format identifier of the user-defined section, <c>FMTID_UserDefinedProperties</c>.</summary>
    private static readonly Guid UserDefinedFormatId = new("D5CDD505-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>
    /// Verifies that an ANSI string is written as an [MS-OLEPS] CodePageString: its size counts a terminating null,
    /// which follows the characters.
    /// </summary>
    [TestMethod]
    public void Write_WhenAnsiString_ShouldTerminateWithNul()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(2, OlePropertyValue.Create("abc"));
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        byte[] bytes = set.ToArray();
        int value = FindPropertyOffsets(bytes, 0)[2];

        Assert.AreEqual(0x001E, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(value)));
        Assert.AreEqual(4u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(value + 4)));
        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63, 0x00 }, bytes.AsSpan(value + 8, 4).ToArray());
    }

    /// <summary>
    /// Verifies that in a written two-section set the second locator addresses the second section's size field, and that
    /// each property offset, taken from its own section's start, addresses that property's type word.
    /// </summary>
    [TestMethod]
    public void Write_WhenTwoSections_ShouldPointEachOffsetAtItsSection()
    {
        var first = new OlePropertySection(TestFormatId, 1252);
        first.Set(2, OlePropertyValue.Create("first"));
        first.Set(3, OlePropertyValue.Create(1));
        var second = new OlePropertySection(UserDefinedFormatId, 1252);
        second.Set(2, OlePropertyValue.Create(2L));
        second.SetName(2, "Second");
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(first);
        set.AddSection(second);

        byte[] bytes = set.ToArray();
        int firstOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28 + 16));
        int secondOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28 + 20 + 16));
        int firstSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(firstOffset));
        int secondSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(secondOffset));

        Assert.AreEqual(UserDefinedFormatId, new Guid(bytes.AsSpan(28 + 20, 16)));
        Assert.IsLessThanOrEqualTo(secondOffset, firstOffset + firstSize);
        Assert.AreEqual(bytes.Length - secondOffset, secondSize);
        Assert.AreEqual(0x001E, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(FindPropertyOffsets(bytes, 0)[2])));
        Assert.AreEqual(0x0003, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(FindPropertyOffsets(bytes, 0)[3])));
        Assert.AreEqual(0x0014, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(FindPropertyOffsets(bytes, 1)[2])));
    }

    /// <summary>
    /// Verifies that each section of a written set holds exactly its own properties when the two sections hold
    /// different numbers of them.
    /// </summary>
    [TestMethod]
    public void Write_WhenSectionsHoldDifferentPropertyCounts_ShouldWriteEachSectionsOwnProperties()
    {
        var first = new OlePropertySection(TestFormatId, 1252);
        first.Set(2, OlePropertyValue.Create("two"));
        first.Set(3, OlePropertyValue.Create(3));
        first.Set(4, OlePropertyValue.Create(4));
        var second = new OlePropertySection(UserDefinedFormatId, 1252);
        second.Set(2, OlePropertyValue.Create("custom"));
        second.SetName(2, "Custom");
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(first);
        set.AddSection(second);

        OlePropertySet parsed = OlePropertySet.Parse(set.ToArray());

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, parsed.Sections[0].Properties.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, parsed.Sections[1].Properties.Keys.ToArray());
        Assert.AreEqual("two", parsed.Sections[0][2]!.AsString());
        Assert.AreEqual("custom", parsed.Sections[1].GetNamedProperties()["Custom"].AsString());
    }

    /// <summary>
    /// Reads the property identifiers of one section of a written set and the absolute offsets of their values.
    /// </summary>
    /// <param name="bytes">The written property set.</param>
    /// <param name="sectionIndex">The zero-based index of the section.</param>
    /// <returns>Each property identifier mapped to the absolute offset of its value.</returns>
    private static Dictionary<int, int> FindPropertyOffsets(byte[] bytes, int sectionIndex)
    {
        int section = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28 + (20 * sectionIndex) + 16));
        int count = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(section + 4));
        var offsets = new Dictionary<int, int>(count);
        for (int i = 0; i < count; i++)
        {
            int pair = section + 8 + (8 * i);
            offsets[(int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pair))] =
                section + (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pair + 4));
        }

        return offsets;
    }
}
