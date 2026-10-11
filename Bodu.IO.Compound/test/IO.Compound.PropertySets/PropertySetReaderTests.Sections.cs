// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.Sections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetReaderTests
{
    /// <summary>
    /// Verifies that a value stored after the end its section's size field declares is read from its own size field
    /// rather than rejected.
    /// </summary>
    /// <remarks>
    /// The section declares 32 bytes, its header, two identifier and offset pairs and the code page, and property 17, a
    /// <c>VT_CF</c> of eight bytes, lies after them.
    /// </remarks>
    [TestMethod]
    public void Parse_WhenValueLiesPastTheDeclaredSectionSize_ShouldReadIt()
    {
        byte[] clipboard = PropertySetBytes.Typed(0x0047, PropertySetBytes.UInt32(8), PropertySetBytes.UInt32(0xFFFFFFFF), PropertySetBytes.UInt32(3));
        byte[] bytes = PropertySetBytes.SingleWithDeclaredSize(
            PropertySetBytes.SummaryInformation,
            32,
            (1, PropertySetBytes.CodePage(1252)),
            (17, clipboard));

        OlePropertySet parsed = OlePropertySet.Parse(bytes);

        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00 }, parsed[17]?.AsBytes());
    }
}
