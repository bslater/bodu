// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Sections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that a value stored after the end its section's size field declares is read from its own size field
    /// rather than rejected.
    /// </summary>
    /// <remarks>
    /// The section declares 32 bytes, its header, two identifier and offset pairs and the code page, and property 17, a
    /// <c>VT_CF</c> of eight bytes, lies after them.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenValueLiesPastTheDeclaredSectionSize_ShouldReadIt()
    {
        byte[] clipboard = PropertySetBytes.Typed(0x0047, PropertySetBytes.UInt32(8), PropertySetBytes.UInt32(0xFFFFFFFF), PropertySetBytes.UInt32(3));
        byte[] bytes = PropertySetBytes.SingleWithDeclaredSize(
            PropertySetBytes.SummaryInformation,
            32,
            (1, PropertySetBytes.CodePage(1252)),
            (17, clipboard));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00 }, parsed[17]?.AsBytes());
    }

    /// <summary>
    /// Verifies that property identifiers at and above 0x80000000, such as PID_LOCALE (0x80000000) and PID_BEHAVIOR
    /// (0x80000003), are read as the unsigned 32-bit values [MS-OLEPS] defines, keyed by their unchecked
    /// <see cref="int" /> value, and that the properties after them still read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyIdIsAtOrAbove0x80000000_ShouldReadIt()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.UserDefinedProperties,
            (1, PropertySetBytes.CodePage(1252)),
            (unchecked((int)0x80000000), PropertySetBytes.Typed(0x0013, PropertySetBytes.UInt32(0x0409))),
            (unchecked((int)0x80000003), PropertySetBytes.Typed(0x0013, PropertySetBytes.UInt32(1))),
            (2, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsTrue(parsed.TryGetValue(unchecked((int)0x80000000), out OlePropertyValue? locale));
        Assert.AreEqual(0x0409u, locale.Value);
        Assert.IsTrue(parsed.TryGetValue(unchecked((int)0x80000003), out OlePropertyValue? behavior));
        Assert.AreEqual(1u, behavior.Value);
        Assert.AreEqual(7, parsed[2]!.AsInt32());
    }

    /// <summary>
    /// Verifies that a section locator whose offset, 0x80000000, does not fit in an <see cref="int" /> fails the set
    /// with <see cref="CompoundFileFormatException" /> and <see cref="CompoundFileError.InvalidPropertySet" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionOffsetExceedsInt32_ShouldThrowInvalidPropertySet()
    {
        byte[] bytes = PropertySetBytes.Single(PropertySetBytes.SummaryInformation, (1, PropertySetBytes.CodePage(1252)));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28 + 16), 0x80000000);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            _ = OlePropertySet.Parse(bytes);
        });

        Assert.AreEqual(CompoundFileError.InvalidPropertySet, ex.Category);
    }

    /// <summary>
    /// Verifies that a section whose size field, 0xFFFFFFFF, does not fit in an <see cref="int" /> fails the set with
    /// <see cref="CompoundFileFormatException" /> and <see cref="CompoundFileError.InvalidPropertySet" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionSizeExceedsInt32_ShouldThrowInvalidPropertySet()
    {
        byte[] bytes = PropertySetBytes.SingleWithDeclaredSize(PropertySetBytes.SummaryInformation, unchecked((int)0xFFFFFFFF), (1, PropertySetBytes.CodePage(1252)));

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            _ = OlePropertySet.Parse(bytes);
        });

        Assert.AreEqual(CompoundFileError.InvalidPropertySet, ex.Category);
    }

    /// <summary>
    /// Verifies that a property whose offset, 0x80000000, does not fit in an <see cref="int" /> is dropped as malformed
    /// while the property after it still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyOffsetExceedsInt32_ShouldDropOnlyThatProperty()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Int32(5)),
            (3, PropertySetBytes.Int32(7)));

        // The section follows the 28-byte header and one locator; the offset of the second pair, property 2, follows its
        // identifier.
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48 + 8 + 8 + 4), 0x80000000);

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsFalse(parsed.TryGetValue(2, out _));
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }
}
