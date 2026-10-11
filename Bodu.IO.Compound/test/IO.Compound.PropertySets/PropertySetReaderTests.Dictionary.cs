// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Dictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that a dictionary whose third entry runs past the end of the stream still names the properties its first
    /// two entries name.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDictionaryIsBroken_ShouldKeepTheNamesBeforeTheBreak()
    {
        byte[] dictionary =
        [
            .. PropertySetBytes.UInt32(3),
            .. PropertySetBytes.UInt32(2), .. PropertySetBytes.UInt32(4), .. PropertySetBytes.Latin1("One\0"),
            .. PropertySetBytes.UInt32(3), .. PropertySetBytes.UInt32(4), .. PropertySetBytes.Latin1("Two\0"),
            .. PropertySetBytes.UInt32(4), .. PropertySetBytes.UInt32(0x7FFFFFF0),
        ];
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.UserDefinedProperties,
            (0, dictionary),
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Int32(11)),
            (3, PropertySetBytes.Int32(22)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);
        IReadOnlyDictionary<string, OlePropertyValue> named = parsed.Sections[0].GetNamedProperties();

        Assert.HasCount(2, named);
        Assert.AreEqual(11, named["One"].AsInt32());
        Assert.AreEqual(22, named["Two"].AsInt32());
    }

    /// <summary>
    /// Verifies that the dictionary of a code page 1200 section reads each name as its length in UTF-16 characters, each
    /// entry padded to a multiple of four bytes.
    /// </summary>
    /// <remarks>
    /// The first name, <c>Ab</c> and its null, has length 3: six bytes and two of padding. The second, <c>Cde</c> and its
    /// null, has length 4: eight bytes and none.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenDictionaryNamesAreUtf16_ShouldReadCharacterLengthAndPadding()
    {
        byte[] dictionary =
        [
            .. PropertySetBytes.UInt32(2),
            .. PropertySetBytes.UInt32(2), .. PropertySetBytes.UInt32(3), .. PropertySetBytes.Utf16("Ab\0"), 0, 0,
            .. PropertySetBytes.UInt32(3), .. PropertySetBytes.UInt32(4), .. PropertySetBytes.Utf16("Cde\0"),
        ];
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.UserDefinedProperties,
            (0, dictionary),
            (1, PropertySetBytes.CodePage(1200)),
            (2, PropertySetBytes.Int32(11)),
            (3, PropertySetBytes.Int32(22)));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        Assert.AreEqual("Ab", parsed.Sections[0].PropertyNames[2]);
        Assert.AreEqual("Cde", parsed.Sections[0].PropertyNames[3]);
    }
}
