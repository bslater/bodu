// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Vectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that the document parts of a document summary, stored as [MS-OSHARED] requires as strings with no
    /// padding between them, read element by element.
    /// </summary>
    /// <remarks>
    /// [MS-OSHARED] 2.3.3.2.2.1 requires GKPIDDSI_DOCPARTS (property 13) to be a VtVecUnalignedLpstr, whose strings are
    /// UnalignedLpstr values (2.3.3.1.9), which differ from the [MS-OLEPS] CodePageString "in the absence of padding
    /// bytes" (2.3.3.1.5). Here the vector holds <c>Sheet1</c>, <c>Sheet22</c> and <c>Chart</c>, each with its null, one
    /// after another, and only the vector is padded to a multiple of four bytes.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenDocumentPartsHoldUnalignedStrings_ShouldReadEachPart()
    {
        byte[] parts = PropertySetBytes.Typed(
            0x101E,
            PropertySetBytes.UInt32(3),
            PropertySetBytes.UInt32(7),
            PropertySetBytes.Latin1("Sheet1\0"),
            PropertySetBytes.UInt32(8),
            PropertySetBytes.Latin1("Sheet22\0"),
            PropertySetBytes.UInt32(6),
            PropertySetBytes.Latin1("Chart\0"),
            [0, 0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.DocumentSummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (13, parts));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "Sheet1", "Sheet22", "Chart" }, (object?[]?)parsed[13]?.Value);
    }

    /// <summary>
    /// Verifies that the heading pairs of a document summary, a vector of variants whose strings [MS-OSHARED] stores
    /// with no padding, read element by element.
    /// </summary>
    /// <remarks>
    /// [MS-OSHARED] 2.3.3.1.12 to 2.3.3.1.15 define a heading pair's string as a VtUnalignedString. Here property 12
    /// holds a <c>VT_LPSTR</c> of size 6, <c>Title</c> and its null, followed at once by the <c>VT_I4</c> 1.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenHeadingPairsHoldUnalignedStrings_ShouldReadEachElement()
    {
        byte[] pairs = PropertySetBytes.Typed(
            0x100C,
            PropertySetBytes.UInt32(2),
            PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(6), PropertySetBytes.Latin1("Title\0")),
            PropertySetBytes.Int32(1),
            [0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.DocumentSummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (12, pairs));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "Title", 1 }, (object?[]?)parsed[12]?.Value);
    }

    /// <summary>
    /// Verifies that the strings of a <c>VT_VECTOR | VT_LPSTR</c>, each padded to a multiple of four bytes as [MS-OLEPS]
    /// requires, read element by element, and that the property after the vector still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenVectorStringsNeedPadding_ShouldReadEachElement()
    {
        byte[] strings = PropertySetBytes.Typed(
            0x101E,
            PropertySetBytes.UInt32(2),
            PropertySetBytes.UInt32(3),
            PropertySetBytes.Latin1("ab\0"),
            [0],
            PropertySetBytes.UInt32(6),
            PropertySetBytes.Latin1("abcde\0"),
            [0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, strings),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "ab", "abcde" }, (object?[]?)parsed[2]?.Value);
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that heading pairs holding several pairs, each string unpadded as [MS-OSHARED] stores it, read element
    /// by element.
    /// </summary>
    /// <remarks>
    /// The vector holds the pairs <c>Worksheets</c>, 2 and <c>Named Ranges</c>, 1, the shape Excel writes: each
    /// <c>VT_LPSTR</c> is followed at once by the <c>VT_I4</c> after it, and only the vector is padded.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenHeadingPairsHoldSeveralUnalignedPairs_ShouldReadEachElement()
    {
        byte[] pairs = PropertySetBytes.Typed(
            0x100C,
            PropertySetBytes.UInt32(4),
            PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(11), PropertySetBytes.Latin1("Worksheets\0")),
            PropertySetBytes.Int32(2),
            PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(13), PropertySetBytes.Latin1("Named Ranges\0")),
            PropertySetBytes.Int32(1),
            [0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.DocumentSummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (12, pairs));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "Worksheets", 2, "Named Ranges", 1 }, (object?[]?)parsed[12]?.Value);
    }

    /// <summary>
    /// Verifies that a <c>VT_VECTOR | VT_LPSTR</c> whose first string is padded with zeros to a multiple of four bytes
    /// and whose second is not reads every element.
    /// </summary>
    /// <remarks>
    /// <c>ab</c> and its null take three bytes and one zero byte pads them; <c>Sheet1</c> and its null take seven bytes
    /// and are followed at once by the length of <c>x</c>, whose first byte is not zero.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenVectorMixesPaddedAndUnpaddedStrings_ShouldReadEachElement()
    {
        byte[] strings = PropertySetBytes.Typed(
            0x101E,
            PropertySetBytes.UInt32(3),
            PropertySetBytes.UInt32(3),
            PropertySetBytes.Latin1("ab\0"),
            [0],
            PropertySetBytes.UInt32(7),
            PropertySetBytes.Latin1("Sheet1\0"),
            PropertySetBytes.UInt32(2),
            PropertySetBytes.Latin1("x\0"),
            [0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.DocumentSummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (13, strings));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "ab", "Sheet1", "x" }, (object?[]?)parsed[13]?.Value);
    }

    /// <summary>
    /// Verifies that the strings of a <c>VT_VECTOR | VT_LPWSTR</c>, each padded with zeros to a multiple of four bytes as
    /// [MS-OLEPS] requires, read element by element.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnicodeVectorStringsArePadded_ShouldReadEachElement()
    {
        byte[] strings = PropertySetBytes.Typed(
            0x101F,
            PropertySetBytes.UInt32(2),
            PropertySetBytes.UInt32(3),
            PropertySetBytes.Utf16("ab\0"),
            [0, 0],
            PropertySetBytes.UInt32(4),
            PropertySetBytes.Utf16("abc\0"));
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.DocumentSummaryInformation,
            (1, PropertySetBytes.CodePage(1200)),
            (13, strings));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new object[] { "ab", "abc" }, (object?[]?)parsed[13]?.Value);
    }

    /// <summary>
    /// Verifies that a vector of 1- or 2-byte values, whose elements [MS-OLEPS] 2.15 packs one after another with
    /// padding only after the last, reads element by element, and that the property after it still reads.
    /// </summary>
    /// <param name="typeName">The name of the element type: <c>Int16</c>, <c>UInt16</c>, <c>Boolean</c>, <c>Int8</c> or <c>UInt8</c>.</param>
    [TestMethod]
    [DataRow("Int16")]
    [DataRow("UInt16")]
    [DataRow("Boolean")]
    [DataRow("Int8")]
    [DataRow("UInt8")]
    public void Parse_WhenSmallScalarVectorIsPacked_ShouldReadEachElement(string typeName)
    {
        (ushort type, byte[] elements, object[] expected) = typeName switch
        {
            "Int16" => ((ushort)0x0002, new byte[] { 0x01, 0x00, 0xFE, 0xFF, 0x03, 0x00, 0x00, 0x00 }, new object[] { (short)1, (short)-2, (short)3 }),
            "UInt16" => ((ushort)0x0012, new byte[] { 0x01, 0x00, 0x02, 0x00, 0xFF, 0xFF, 0x00, 0x00 }, new object[] { (ushort)1, (ushort)2, (ushort)65535 }),
            "Boolean" => ((ushort)0x000B, new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00 }, new object[] { true, false, true }),
            "Int8" => ((ushort)0x0010, new byte[] { 0xFF, 0x02, 0xFD, 0x00 }, new object[] { (sbyte)-1, (sbyte)2, (sbyte)-3 }),
            _ => ((ushort)0x0011, new byte[] { 0x00, 0x80, 0xFF, 0x00 }, new object[] { (byte)0, (byte)128, (byte)255 }),
        };
        byte[] vector = PropertySetBytes.Typed((ushort)(type | 0x1000), PropertySetBytes.UInt32(3), elements);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, vector),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(expected, (object?[]?)parsed[2]?.Value, typeName);
        Assert.AreEqual(7, parsed[3]!.AsInt32(), typeName);
    }
}
