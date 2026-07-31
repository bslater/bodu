// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.MultiValued.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that a multi-valued Unicode property round-trips through its length and element streams.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedUnicode_ShouldMaterializeAllElements()
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddMultiValueUnicode(0x3A58, "red", "green", "blue"));

        CollectionAssert.AreEqual(new[] { "red", "green", "blue" }, properties.GetStringArray(0x3A58));
    }

    /// <summary>
    /// Verifies that a multi-valued binary property round-trips, using the 8-byte length-stream entries.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedBinary_ShouldMaterializeAllElements()
    {
        byte[] first = { 0x01 };
        byte[] second = { 0x02, 0x03 };
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddMultiValueBinary(0x1234, first, second));

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x12341102u), out MapiProperty? property));
        var values = (byte[][])property.Value!;
        Assert.AreEqual(2, values.Length);
        CollectionAssert.AreEqual(first, values[0]);
        CollectionAssert.AreEqual(second, values[1]);
    }

    /// <summary>
    /// Verifies that a multi-valued fixed-length property decodes from a single packed stream — no element streams.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedInt32_ShouldDecodePackedStream()
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddMultiValueInt32(0x1234, 1, -2, 3));

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x12341003u), out MapiProperty? property));
        CollectionAssert.AreEqual(new[] { 1, -2, 3 }, (int[])property.Value!);
    }

    /// <summary>
    /// Verifies, against hand-authored raw bytes that bypass the fixture builder, that the multi-valued layouts are
    /// what the decoder assumes: 4-byte length entries for strings, 8-byte for binary, packed elements for fixed
    /// types.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decode_WhenHandAuthoredMultiValueLayouts_ShouldMatchSpecification()
    {
        // MV_UNICODE 0x3A58101F: two elements "ab" (4 bytes) and "c" (2 bytes); 4-byte length entries include the
        // 2-byte terminator per MS-OXMSG §2.1.3.
        var lengths = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(lengths, 6);
        BinaryPrimitives.WriteUInt32LittleEndian(lengths.AsSpan(4), 4);

        // MV_LONG 0x36001003: two packed little-endian elements.
        var packed = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(packed, 7);
        BinaryPrimitives.WriteInt32LittleEndian(packed.AsSpan(4), -8);

        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x3A58101F, (uint)lengths.Length)
            .AddRawStream("__substg1.0_3A58101F", lengths)
            .AddRawStream("__substg1.0_3A58101F-00000000", System.Text.Encoding.Unicode.GetBytes("ab"))
            .AddRawStream("__substg1.0_3A58101F-00000001", System.Text.Encoding.Unicode.GetBytes("c"))
            .AddEntryWithoutStream(0x36001003, (uint)packed.Length)
            .AddRawStream("__substg1.0_36001003", packed));

        CollectionAssert.AreEqual(new[] { "ab", "c" }, properties.GetStringArray(0x3A58));
        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x36001003u), out MapiProperty? longs));
        CollectionAssert.AreEqual(new[] { 7, -8 }, (int[])longs.Value!);
    }

    /// <summary>
    /// Verifies that a length stream whose size is not a whole number of entries skips the property under compatible
    /// validation and throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLengthStreamMisaligned_ShouldOmitOrThrowByValidationLevel()
    {
        var builder = new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x3A58101F, 6)
            .AddRawStream("__substg1.0_3A58101F", new byte[6]);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.IsFalse(tolerant.Contains(new MapiPropertyTag(0x3A58101Fu)));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a missing element stream skips the property under compatible validation and throws under strict
    /// validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenElementStreamMissing_ShouldOmitOrThrowByValidationLevel()
    {
        var lengths = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(lengths, 4);
        var builder = new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x3A58101F, 4)
            .AddRawStream("__substg1.0_3A58101F", lengths);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.IsFalse(tolerant.Contains(new MapiPropertyTag(0x3A58101Fu)));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }
}
