// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Variants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>The format identifier used for the test sections.</summary>
    private static readonly Guid TestFormatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>The property identifier the single test property is stored under.</summary>
    private const int TestPropertyId = 10;

    /// <summary>The property identifier of a companion property used to prove the section survives.</summary>
    private const int CompanionPropertyId = 11;

    /// <summary>
    /// Serializes a set carrying the property under test plus a companion, and locates the byte offset of the
    /// property's type word.
    /// </summary>
    /// <param name="value">The property value to author.</param>
    /// <param name="typeOffset">Receives the offset of the property's two-byte type word.</param>
    /// <returns>The serialized property-set bytes.</returns>
    private static byte[] AuthorAndLocate(OlePropertyValue value, out int typeOffset)
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(TestPropertyId, value);
        section.Set(CompanionPropertyId, OlePropertyValue.Create("companion"));
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        byte[] bytes = set.ToArray();

        // Header is 28 bytes, then one 20-byte section locator whose trailing uint is the section offset. The
        // section begins with its byte length and property count, followed by (id, offset) pairs. The section
        // also emits its code page as PID 1, so the pair table is searched by identifier rather than indexed -
        // the authored property is not the first entry. Each recorded offset is relative to the section start.
        int sectionOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28 + 16));
        int count = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sectionOffset + 4));

        for (int i = 0; i < count; i++)
        {
            int pair = sectionOffset + 8 + (i * 8);
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pair)) != TestPropertyId)
                continue;

            typeOffset = sectionOffset + (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pair + 4));
            return bytes;
        }

        throw new InvalidOperationException($"Property {TestPropertyId} was not found in the serialized section.");
    }

    /// <summary>
    /// Verifies that a property whose declared type the reader does not implement is dropped while the rest of the
    /// section still parses, rather than decoded as an adjacent type or taking the whole set down with it.
    /// </summary>
    /// <remarks>
    /// The value decoder rejects an unknown type word, but each property is decoded inside its own try/catch so one
    /// unreadable entry costs only that entry. A document produced by another writer can carry variant types this
    /// reader has no mapping for, and losing every other property in the section over one of them would be the wrong
    /// trade.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenPropertyTypeIsUnsupported_ShouldDropOnlyThatProperty()
    {
        byte[] bytes = AuthorAndLocate(OlePropertyValue.Create(42), out int typeOffset);

        // VT_UNKNOWN (13) is a real OLE variant type that this reader has no decoding for.
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(typeOffset), 13);

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsFalse(parsed.TryGetValue(TestPropertyId, out _));
        Assert.AreEqual("companion", parsed[CompanionPropertyId]!.AsString());
    }

    /// <summary>
    /// Verifies that a <c>VT_DATE</c> whose OLE automation value falls outside the representable range decodes to
    /// <see langword="null" /> rather than propagating the conversion failure, so one bad date does not make the
    /// whole property set unreadable.
    /// </summary>
    /// <param name="oaDate">The out-of-range OLE automation date.</param>
    [TestMethod]
    [DataRow(1e9)]
    [DataRow(-1e9)]
    public void Parse_WhenDateIsOutOfRange_ShouldDecodeToNull(double oaDate)
    {
        byte[] bytes = AuthorAndLocate(
            new OlePropertyValue { Type = OlePropertyType.Date, Value = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero) },
            out int typeOffset);

        // The 8-byte OLE automation double follows the 2-byte type word and its 2-byte padding.
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(typeOffset + 4), oaDate);

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsNotNull(parsed[TestPropertyId]);
        Assert.IsNull(parsed[TestPropertyId]!.Value);
    }

    /// <summary>
    /// Verifies that a representable date still decodes, so the out-of-range test above is not passing merely
    /// because the patch offset is wrong.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDateIsInRange_ShouldDecodeToThatInstant()
    {
        var expected = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);
        byte[] bytes = AuthorAndLocate(
            new OlePropertyValue { Type = OlePropertyType.Date, Value = expected },
            out int typeOffset);

        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(typeOffset + 4), expected.UtcDateTime.ToOADate());

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(expected, parsed[TestPropertyId]!.AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a <c>VT_ARRAY</c> property, a type the reader does not implement, is dropped while the rest of the
    /// section still parses, rather than read as a scalar of its element type.
    /// </summary>
    /// <remarks>
    /// [MS-OLEPS] 2.15 defines <c>VT_ARRAY | VT_I4</c> (0x2003) as an ArrayHeader followed by the elements: here the
    /// element type 3, one dimension of two elements from index 0, then 10 and 20. Clearing the 0x2000 bit would read the
    /// ArrayHeader's first word, 3, as a <c>VT_I4</c>.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenPropertyIsArray_ShouldDropOnlyThatProperty()
    {
        byte[] array = PropertySetBytes.Typed(
            0x2003,
            PropertySetBytes.UInt32(3),
            PropertySetBytes.UInt32(1),
            PropertySetBytes.UInt32(2),
            PropertySetBytes.UInt32(0),
            PropertySetBytes.UInt32(10),
            PropertySetBytes.UInt32(20));
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, array),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsFalse(parsed.TryGetValue(2, out _));
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that a <c>VT_CY</c> property, the OLE Automation <c>CURRENCY</c> held as a signed 64-bit count of
    /// ten-thousandths, reads as that count divided by 10,000.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyIsCurrency_ShouldScaleByTenThousand()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x0006, PropertySetBytes.Int64(15000))),
            (3, PropertySetBytes.Typed(0x0006, PropertySetBytes.Int64(-25000))));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(1.5m, parsed[2]!.Value);
        Assert.AreEqual(-2.5m, parsed[3]!.Value);
    }

    /// <summary>
    /// Verifies that a <c>VT_NULL</c> property, which has no value bytes, reads as a present property whose value is
    /// <see langword="null" />, and that the property after it still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyIsNull_ShouldReadNullValue()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x0001)),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsTrue(parsed.TryGetValue(2, out OlePropertyValue? value));
        Assert.IsNull(value.Value);
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that a property whose type word carries a modifier other than <c>VT_VECTOR</c> (<c>VT_ARRAY</c> 0x2000,
    /// <c>VT_BYREF</c> 0x4000 or <c>VT_RESERVED</c> 0x8000), alone or with <c>VT_VECTOR</c>, is dropped like any type the
    /// reader does not implement, while the property after it still reads.
    /// </summary>
    /// <param name="type">The type word of the dropped property.</param>
    [TestMethod]
    [DataRow((ushort)0x4003)]
    [DataRow((ushort)0x8003)]
    [DataRow((ushort)0x3003)]
    [DataRow((ushort)0x6003)]
    public void Parse_WhenTypeCarriesAnUnsupportedModifier_ShouldDropOnlyThatProperty(ushort type)
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(type, PropertySetBytes.UInt32(1), PropertySetBytes.UInt32(10), PropertySetBytes.UInt32(20))),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsFalse(parsed.TryGetValue(2, out _));
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that a <c>VT_VECTOR | VT_VARIANT</c> whose element declares <c>VT_ARRAY | VT_I4</c> is dropped like an
    /// unsupported type, rather than read as a <c>VT_I4</c>, while the property after it still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenVariantElementIsArray_ShouldDropOnlyThatProperty()
    {
        byte[] variants = PropertySetBytes.Typed(
            0x100C,
            PropertySetBytes.UInt32(1),
            PropertySetBytes.Typed(0x2003, PropertySetBytes.UInt32(3), PropertySetBytes.UInt32(1), PropertySetBytes.UInt32(1), PropertySetBytes.UInt32(0), PropertySetBytes.UInt32(10)));
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, variants),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.IsFalse(parsed.TryGetValue(2, out _));
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }
}
