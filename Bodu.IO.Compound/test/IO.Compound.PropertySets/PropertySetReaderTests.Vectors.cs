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
}
