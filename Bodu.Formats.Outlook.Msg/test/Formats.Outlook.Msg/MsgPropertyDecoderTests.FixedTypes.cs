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
    /// Verifies that a zero FILETIME — the conventional "unset time stamp" — materializes as a present property with
    /// a <see langword="null" /> value at every validation level, rather than being treated as corruption.
    /// </summary>
    /// <param name="validationLevel">The validation level to decode under.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    public void Decode_WhenFileTimeZero_ShouldMaterializeNullValue(CompoundValidationLevel validationLevel)
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder().AddFixedEntry(0x12340040, 0), validationLevel);

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x12340040u), out MapiProperty? property));
        Assert.IsNull(property.Value);
        Assert.IsNull(properties.GetDateTime(0x1234));
    }

    /// <summary>
    /// Verifies that the legal <c>PT_NULL</c> and <c>PT_UNSPECIFIED</c> types materialize as present properties with
    /// a <see langword="null" /> value at every validation level, rather than being rejected as unknown types.
    /// </summary>
    /// <param name="validationLevel">The validation level to decode under.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    public void Decode_WhenNullOrUnspecifiedType_ShouldMaterializeNullValue(CompoundValidationLevel validationLevel)
    {
        MapiPropertyCollection properties = Decode(
            new MsgFixtureBuilder().AddFixedEntry(0x12340001, 0).AddFixedEntry(0x12350000, 0),
            validationLevel);

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x12340001u), out MapiProperty? nullTyped));
        Assert.IsNull(nullTyped.Value);
        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x12350000u), out MapiProperty? unspecified));
        Assert.IsNull(unspecified.Value);
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
