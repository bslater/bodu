// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Strings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that a <c>VT_LPSTR</c> of size 0 reads as an empty string, and that the property after it still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnsiStringIsEmpty_ShouldReturnEmptyString()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(0))),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(string.Empty, parsed[2]!.AsString());
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that the terminating null a <c>VT_LPSTR</c> carries, and counts in its size, is not part of the value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnsiStringIsNulTerminated_ShouldOmitTheTerminator()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(4), [0x61, 0x62, 0x63, 0x00])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("abc", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that a <c>VT_LPWSTR</c> reads as the UTF-16 text its character count covers, without its terminating
    /// null.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyIsLpwstr_ShouldDecodeUtf16Text()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001F, PropertySetBytes.UInt32(5), PropertySetBytes.Utf16("cafe\0"), [0, 0])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("cafe", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that the null characters a string value carries after its text, beyond the one that terminates it, are
    /// trimmed from <c>VT_LPSTR</c> and <c>VT_LPWSTR</c> values alike.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringCarriesTrailingNulls_ShouldTrimThem()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(8), PropertySetBytes.Latin1("ABC\0\0\0\0\0"))),
            (3, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(6), PropertySetBytes.Latin1("abc\0\0\0"), [0, 0])),
            (4, PropertySetBytes.Typed(0x001F, PropertySetBytes.UInt32(5), PropertySetBytes.Utf16("abc\0\0"), [0, 0])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("ABC", parsed[2]!.AsString());
        Assert.AreEqual("abc", parsed[3]!.AsString());
        Assert.AreEqual("abc", parsed[4]!.AsString());
    }

    /// <summary>
    /// Verifies that a <c>VT_LPWSTR</c> whose writer aligned the string, rather than the property, to a four-byte
    /// boundary is read from that boundary when the length at the expected place is implausible.
    /// </summary>
    /// <remarks>
    /// The code page value is followed by two filler bytes, so property 2 begins two bytes past a four-byte boundary of
    /// the stream. Its type word and padding are followed by two more zero bytes and then, on the next boundary, the
    /// length 3 and the characters <c>a</c>, <c>b</c> and a null. Read from the expected place, the length would be
    /// 0x00030000 characters, more than the stream holds.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenUnicodeStringBeginsOnTheNextBoundary_ShouldReadIt()
    {
        byte[] codePage = [.. PropertySetBytes.CodePage(1252), 0, 0];
        byte[] text = PropertySetBytes.Typed(0x001F, [0, 0], PropertySetBytes.UInt32(3), PropertySetBytes.Utf16("ab\0"), [0, 0]);
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, codePage),
            (2, text),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("ab", parsed[2]?.AsString());
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }

    /// <summary>
    /// Verifies that a <c>VT_LPWSTR</c> whose character count is 0 reads as an empty string, and that the property after
    /// it still reads.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnicodeStringIsEmpty_ShouldReturnEmptyString()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001F, PropertySetBytes.UInt32(0))),
            (3, PropertySetBytes.Int32(7)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(string.Empty, parsed[2]!.AsString());
        Assert.AreEqual(7, parsed[3]!.AsInt32());
    }
}
