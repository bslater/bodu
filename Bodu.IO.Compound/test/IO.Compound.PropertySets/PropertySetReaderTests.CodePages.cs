// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.CodePages.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that a section that declares no code page reads with code page 1252, as
    /// <see cref="OlePropertySection.CodePage" /> documents, and decodes its strings with it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionHasNoCodePageProperty_ShouldDefaultTo1252()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(5), [0x63, 0x61, 0x66, 0xE9, 0x00], [0, 0, 0])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(1252, parsed.Sections[0].CodePage);
        Assert.AreEqual("café", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that each section of a two-section set decodes its strings with the code page that section declares.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionsDeclareDifferentCodePages_ShouldDecodeEachWithItsOwn()
    {
        byte[] bytes = PropertySetBytes.Sections(
            (PropertySetBytes.DocumentSummaryInformation,
                [(1, PropertySetBytes.CodePage(1252)), (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(4), PropertySetBytes.Latin1("abc\0")))],
                null),
            (PropertySetBytes.UserDefinedProperties,
                [(1, PropertySetBytes.CodePage(1200)), (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(8), PropertySetBytes.Utf16("xyz\0")))],
                null));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(1252, parsed.Sections[0].CodePage);
        Assert.AreEqual("abc", parsed.Sections[0][2]!.AsString());
        Assert.AreEqual(1200, parsed.Sections[1].CodePage);
        Assert.AreEqual("xyz", parsed.Sections[1][2]!.AsString());
    }

    /// <summary>
    /// Verifies that a section declaring code page 1252 decodes the bytes Windows-1252 and Latin-1 map differently as
    /// their Windows-1252 characters: 0x92 as U+2019 and 0x80 as U+20AC, not as the C1 controls U+0092 and U+0080.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCodePageIs1252_ShouldDecodeWindows1252Characters()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(7), [0x69, 0x74, 0x92, 0x73, 0x20, 0x80, 0x00], [0])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("it\u2019s \u20AC", parsed[2]!.AsString());
    }

    /// <summary>
    /// Verifies that a section declaring the double-byte code page 932 (Shift-JIS) decodes its ANSI strings with that
    /// code page, two bytes to a character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCodePageIsShiftJis_ShouldDecodeDoubleByteText()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(932)),
            (2, PropertySetBytes.Typed(0x001E, PropertySetBytes.UInt32(7), [0x93, 0xFA, 0x96, 0x7B, 0x8C, 0xEA, 0x00], [0])));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual(932, parsed.Sections[0].CodePage);
        Assert.AreEqual("\u65E5\u672C\u8A9E", parsed[2]!.AsString());
    }
}
