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
}
