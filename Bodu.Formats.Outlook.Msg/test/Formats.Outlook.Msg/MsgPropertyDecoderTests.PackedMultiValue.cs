// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.PackedMultiValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the packed multi-value decoder across every fixed-length element type.
/// </summary>
/// <remarks>
/// Every fixed-length multi-value property shares one on-disk layout - a single stream of packed little-endian
/// elements, with the width implied by the type - so the decoder is a table of element widths and a switch of readers
/// that must agree with it. Only the 32-bit integer arm was covered, which is the one case where a wrong width in the
/// table would still produce plausible output for the wrong reason. Each type is asserted against values that would
/// decode differently under a neighbouring width or a different signedness.
/// </remarks>
public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Concatenates the supplied element byte blocks.
    /// </summary>
    /// <param name="elements">The packed elements.</param>
    /// <returns>The concatenated payload.</returns>
    private static byte[] Pack(params byte[][] elements) =>
        [.. elements.SelectMany(static e => e)];

    /// <summary>
    /// Encodes a 64-bit little-endian value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The eight-byte encoding.</returns>
    private static byte[] Int64Bytes(long value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);

        return bytes;
    }

    /// <summary>
    /// Decodes a packed multi-value property and returns its value.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="type">The element type.</param>
    /// <param name="packed">The packed payload.</param>
    /// <returns>The decoded value, or <see langword="null" /> when the property was omitted.</returns>
    private static object? DecodePacked(ushort id, MapiPropertyType type, byte[] packed)
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder().AddPackedMultiValue(id, type, packed));
        uint tag = ((uint)id << 16) | (uint)type | 0x1000u;

        return properties.TryGetValue(new MapiPropertyTag(tag), out MapiProperty? property) ? property.Value : null;
    }

    /// <summary>
    /// Verifies that 16-bit elements decode at two bytes each, including a negative value that would read as a large
    /// positive one if the width or signedness were wrong.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedInt16_ShouldDecodeTwoByteElements()
    {
        byte[] packed = Pack([0x01, 0x00], [0xFF, 0xFF], [0x00, 0x80]);

        CollectionAssert.AreEqual(
            new short[] { 1, -1, short.MinValue },
            (short[])DecodePacked(0x1234, MapiPropertyType.Int16, packed)!);
    }

    /// <summary>
    /// Verifies that 64-bit integer elements decode at eight bytes each, including a value beyond 32 bits that a
    /// narrower read would truncate.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedInt64_ShouldDecodeEightByteElements()
    {
        byte[] packed = Pack(Int64Bytes(1), Int64Bytes(-1), Int64Bytes(0x1_0000_0000L));

        CollectionAssert.AreEqual(
            new[] { 1L, -1L, 0x1_0000_0000L },
            (long[])DecodePacked(0x1234, MapiPropertyType.Int64, packed)!);
    }

    /// <summary>
    /// Verifies that single-precision elements decode from their four-byte bit patterns rather than being read as
    /// integers.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedFloat_ShouldDecodeBitPatterns()
    {
        byte[] packed = Pack(
            BitConverter.GetBytes(1.5f),
            BitConverter.GetBytes(-2.25f));

        CollectionAssert.AreEqual(
            new[] { 1.5f, -2.25f },
            (float[])DecodePacked(0x1234, MapiPropertyType.Float, packed)!);
    }

    /// <summary>
    /// Verifies that double-precision elements decode from their eight-byte bit patterns.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedDouble_ShouldDecodeBitPatterns()
    {
        byte[] packed = Pack(
            BitConverter.GetBytes(1.5d),
            BitConverter.GetBytes(-2.25d));

        CollectionAssert.AreEqual(
            new[] { 1.5d, -2.25d },
            (double[])DecodePacked(0x1234, MapiPropertyType.Double, packed)!);
    }

    /// <summary>
    /// Verifies that currency elements are scaled by the fixed 10,000 divisor MAPI defines, rather than surfacing as
    /// raw integers.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedCurrency_ShouldScaleByTenThousand()
    {
        byte[] packed = Pack(Int64Bytes(12345), Int64Bytes(-500000));

        CollectionAssert.AreEqual(
            new[] { 1.2345m, -50m },
            (decimal[])DecodePacked(0x1234, MapiPropertyType.Currency, packed)!);
    }

    /// <summary>
    /// Verifies that timestamp elements decode from FILETIME ticks to the instants they name.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedSystemTime_ShouldDecodeFileTimes()
    {
        var first = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);
        var second = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero);

        byte[] packed = Pack(
            Int64Bytes(first.UtcDateTime.ToFileTimeUtc()),
            Int64Bytes(second.UtcDateTime.ToFileTimeUtc()));

        var decoded = (DateTimeOffset[])DecodePacked(0x1234, MapiPropertyType.SystemTime, packed)!;

        Assert.AreEqual(2, decoded.Length);
        Assert.AreEqual(first, decoded[0].ToUniversalTime());
        Assert.AreEqual(second, decoded[1].ToUniversalTime());
    }

    /// <summary>
    /// Verifies that a timestamp element outside the representable range causes the property to be omitted rather
    /// than faulting the whole message.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedSystemTimeIsOutOfRange_ShouldOmitTheProperty()
    {
        byte[] packed = Pack(Int64Bytes(DateTimeOffset.UtcNow.UtcDateTime.ToFileTimeUtc()), Int64Bytes(long.MaxValue));

        Assert.IsNull(DecodePacked(0x1234, MapiPropertyType.SystemTime, packed));
    }

    /// <summary>
    /// Verifies that GUID elements decode at sixteen bytes each.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMultiValuedGuid_ShouldDecodeSixteenByteElements()
    {
        var first = new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9");
        var second = Guid.Empty;

        byte[] packed = Pack(first.ToByteArray(), second.ToByteArray());

        CollectionAssert.AreEqual(
            new[] { first, second },
            (Guid[])DecodePacked(0x1234, MapiPropertyType.Guid, packed)!);
    }

    /// <summary>
    /// Verifies that a payload whose length is not a whole number of elements is rejected, since the decoder cannot
    /// tell which elements it holds - a partial trailing element would otherwise be read from adjacent bytes.
    /// </summary>
    /// <param name="type">The element type.</param>
    /// <param name="length">A payload length that is not a multiple of the element width.</param>
    [TestMethod]
    [DataRow(MapiPropertyType.Int16, 3)]
    [DataRow(MapiPropertyType.Int32, 6)]
    [DataRow(MapiPropertyType.Int64, 12)]
    [DataRow(MapiPropertyType.Guid, 20)]
    public void Decode_WhenPackedPayloadIsMisaligned_ShouldOmitTheProperty(MapiPropertyType type, int length)
    {
        Assert.IsNull(DecodePacked(0x1234, type, new byte[length]));
    }

    /// <summary>
    /// Verifies that an empty payload decodes to an empty array rather than being treated as malformed, since a
    /// multi-valued property with no elements is well formed.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPackedPayloadIsEmpty_ShouldDecodeToAnEmptyArray()
    {
        Assert.AreEqual(0, ((int[])DecodePacked(0x1234, MapiPropertyType.Int32, [])!).Length);
    }
}
