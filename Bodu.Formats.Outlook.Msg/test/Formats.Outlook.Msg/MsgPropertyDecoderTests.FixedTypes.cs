// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.FixedTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that every fixed-length property type decodes its inline value to the expected CLR value.
    /// </summary>
    /// <param name="kat">The known-answer row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(FixedTypeKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_WhenFixedTypeEntry_ShouldMaterializeExpectedValue(MsgDecodeKat kat)
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder().AddFixedEntry(kat.Tag, kat.Raw));

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(kat.Tag), out MapiProperty? property));
        Assert.AreEqual(kat.Expected, property.Value);
    }

    /// <summary>
    /// Verifies that a zero FILETIME is omitted under compatible validation and throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenFileTimeZero_ShouldOmitOrThrowByValidationLevel()
    {
        var builder = new MsgFixtureBuilder().AddFixedEntry(0x12340040, 0);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.IsFalse(tolerant.Contains(new MapiPropertyTag(0x12340040u)));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that an entry with an unknown property type is skipped under compatible validation and throws under
    /// strict validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUnknownTypeCode_ShouldOmitOrThrowByValidationLevel()
    {
        var builder = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Kept")
            .AddFixedEntry(0x123400FE, 42);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.AreEqual("Kept", tolerant.GetString(MapiPropertyIds.Subject));
        Assert.IsFalse(tolerant.Contains(new MapiPropertyTag(0x123400FEu)));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }
}
