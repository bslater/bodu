// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderHardeningTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Test;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies that the OLE property-set reader bounds attacker-controlled count, length, and nesting fields so a crafted
/// property set fails fast as a <see cref="CompoundFileFormatException" /> instead of exhausting memory or the call
/// stack.
/// </summary>
[TestClass]
public class PropertySetReaderHardeningTests
{
    /// <summary>The little-endian byte-order marker at the start of a property-set stream.</summary>
    private const ushort ByteOrderMarker = 0xFFFE;

    /// <summary>The fixed size, in bytes, of the property-set header preceding the section locators.</summary>
    private const int HeaderSize = 28;

    /// <summary>The size, in bytes, of a single section locator (FMTID plus offset).</summary>
    private const int LocatorSize = 20;

    /// <summary>The property type bit that flags a vector value.</summary>
    private const ushort VectorFlag = 0x1000;

    /// <summary>The <c>VT_VARIANT</c> base property type.</summary>
    private const ushort VtVariant = 0x000C;

    /// <summary>The <c>VT_I4</c> base property type.</summary>
    private const ushort VtInt32 = 0x0003;

    /// <summary>
    /// Verifies that a section declaring a property count far larger than the available data is rejected with
    /// <see cref="CompoundFileError.InvalidPropertySet" /> rather than attempting a multi-gigabyte allocation.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Read_WhenPropertyCountExceedsData_ShouldThrowInvalidPropertySet()
    {
        // A tiny stream whose section header claims ~268 million properties: pre-fix this allocated a
        // (int, int)[0x10000000] pair table (OutOfMemoryException); it must now be bounded against the data.
        byte[] data = BuildSingleSection(sectionSize: 8, propertyCount: 0x10000000, extraSectionBytes: 0);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(
            () => _ = PropertySetReader.Read(data));

        Assert.AreEqual(CompoundFileError.InvalidPropertySet, ex.Category);
    }

    /// <summary>
    /// Verifies that a vector value declaring an element count far larger than the available data is skipped (the
    /// section remains usable) rather than attempting a huge allocation that escapes as an
    /// <see cref="OutOfMemoryException" />.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Read_WhenVectorCountExceedsData_ShouldSkipPropertyWithoutAllocating()
    {
        // One property of type (VT_VECTOR | VT_I4) whose element count is 0x10000000. Individual malformed
        // property values are decoded defensively, so the reader must skip it and still return the section.
        byte[] data = BuildVectorProperty(elementCount: 0x10000000);

        OlePropertySet set = PropertySetReader.Read(data);

        Assert.AreEqual(1, set.Sections.Count);
        Assert.AreEqual(0, set.Sections[0].Properties.Count);
    }

    /// <summary>
    /// Verifies that a self-describing <c>VT_VARIANT</c> value nested beyond the reader's depth cap is skipped rather
    /// than recursing until the call stack overflows.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Read_WhenVariantNestingUnbounded_ShouldSkipPropertyWithoutOverflow()
    {
        // A property whose value is VT_VARIANT wrapping VT_VARIANT wrapping … 4096 deep. Pre-fix this recursed
        // once per level and overflowed the stack; the depth cap must convert it into a skipped property.
        byte[] data = BuildNestedVariantProperty(depth: 4096);

        OlePropertySet set = PropertySetReader.Read(data);

        Assert.AreEqual(1, set.Sections.Count);
        Assert.AreEqual(0, set.Sections[0].Properties.Count);
    }

    /// <summary>
    /// Builds a property set with a single section whose header declares the supplied size and property count,
    /// followed by <paramref name="extraSectionBytes" /> of zero padding.
    /// </summary>
    /// <param name="sectionSize">The value written to the section's size field.</param>
    /// <param name="propertyCount">The value written to the section's property-count field.</param>
    /// <param name="extraSectionBytes">Additional zero bytes appended after the 8-byte section header.</param>
    /// <returns>The serialized property-set bytes.</returns>
    private static byte[] BuildSingleSection(int sectionSize, uint propertyCount, int extraSectionBytes)
    {
        int sectionOffset = HeaderSize + LocatorSize;
        byte[] data = new byte[sectionOffset + 8 + extraSectionBytes];

        WriteHeader(data, sectionOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(sectionOffset), (uint)sectionSize);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(sectionOffset + 4), propertyCount);
        return data;
    }

    /// <summary>
    /// Builds a property set with one vector property whose element count is <paramref name="elementCount" />.
    /// </summary>
    /// <param name="elementCount">The declared number of vector elements.</param>
    /// <returns>The serialized property-set bytes.</returns>
    private static byte[] BuildVectorProperty(uint elementCount)
    {
        int sectionOffset = HeaderSize + LocatorSize;

        // Section layout: [size][count=1][pid=2][valueOffset][value: type word][element count].
        int valueOffset = 16;
        byte[] section = new byte[valueOffset + 8];
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(0), (uint)section.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(8), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(12), (uint)valueOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(section.AsSpan(valueOffset), (ushort)(VectorFlag | VtInt32));
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(valueOffset + 4), elementCount);

        return Assemble(sectionOffset, section);
    }

    /// <summary>
    /// Builds a property set with one property whose value is a chain of <paramref name="depth" /> nested
    /// <c>VT_VARIANT</c> type words terminating in a <c>VT_I4</c>.
    /// </summary>
    /// <param name="depth">The number of nested variant levels.</param>
    /// <returns>The serialized property-set bytes.</returns>
    private static byte[] BuildNestedVariantProperty(int depth)
    {
        int sectionOffset = HeaderSize + LocatorSize;
        int valueOffset = 16;

        // Each variant level is a 4-byte type word; the innermost value is a VT_I4 type word plus its 4-byte value.
        byte[] value = new byte[(depth * 4) + 8];
        for (int i = 0; i < depth; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(value.AsSpan(i * 4), VtVariant);
        BinaryPrimitives.WriteUInt16LittleEndian(value.AsSpan(depth * 4), VtInt32);
        BinaryPrimitives.WriteInt32LittleEndian(value.AsSpan((depth * 4) + 4), 42);

        byte[] section = new byte[valueOffset + value.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(0), (uint)section.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(8), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(section.AsSpan(12), (uint)valueOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(section.AsSpan(valueOffset), VtVariant);
        value.AsSpan(4).CopyTo(section.AsSpan(valueOffset + 4));

        return Assemble(sectionOffset, section);
    }

    /// <summary>
    /// Assembles a full property set from a single pre-built section body placed at <paramref name="sectionOffset" />.
    /// </summary>
    /// <param name="sectionOffset">The byte offset of the section body.</param>
    /// <param name="section">The section body bytes.</param>
    /// <returns>The serialized property-set bytes.</returns>
    private static byte[] Assemble(int sectionOffset, byte[] section)
    {
        byte[] data = new byte[sectionOffset + section.Length];
        WriteHeader(data, sectionOffset);
        section.CopyTo(data.AsSpan(sectionOffset));
        return data;
    }

    /// <summary>
    /// Writes the 28-byte property-set header and a single section locator pointing at <paramref name="sectionOffset" />.
    /// </summary>
    /// <param name="data">The destination buffer.</param>
    /// <param name="sectionOffset">The byte offset the locator should reference.</param>
    private static void WriteHeader(byte[] data, int sectionOffset)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0), ByteOrderMarker);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(HeaderSize + 16), (uint)sectionOffset);
    }
}
