// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that a Unicode string, a binary payload, and a GUID decode from their value streams.
    /// </summary>
    [TestMethod]
    public void Decode_WhenVariableLengthProperties_ShouldMaterializeValues()
    {
        var guid = new Guid("00020329-0000-0000-C000-000000000046");
        byte[] payload = { 0xDE, 0xAD, 0xBE, 0xEF };
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Hello world")
            .AddBinary(0x1234, payload)
            .AddGuidValue(0x2345, guid));

        Assert.AreEqual("Hello world", properties.GetString(MapiPropertyIds.Subject));
        CollectionAssert.AreEqual(payload, properties.GetBinary(0x1234)!.Value.ToArray());
        Assert.AreEqual(guid, properties.GetGuid(0x2345));
    }

    /// <summary>
    /// Verifies that a <c>PT_OBJECT</c> entry is surfaced as a marker property with a <see langword="null" /> value
    /// and no stream access.
    /// </summary>
    [TestMethod]
    public void Decode_WhenObjectEntry_ShouldSurfaceMarkerWithNullValue()
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x3701000D, 0xFFFFFFFF));

        Assert.IsTrue(properties.TryGetValue(new MapiPropertyTag(0x3701000Du), out MapiProperty? property));
        Assert.IsNull(property.Value);
    }

    /// <summary>
    /// Verifies that an entry whose value stream is missing is omitted under compatible validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSubstgMissingUnderCompatible_ShouldOmitProperty()
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Kept")
            .AddEntryWithoutStream(0x1234001F, 10));

        Assert.AreEqual("Kept", properties.GetString(MapiPropertyIds.Subject));
        Assert.IsFalse(properties.Contains(new MapiPropertyTag(0x1234001Fu)));
    }

    /// <summary>
    /// Verifies that an entry whose value stream is missing throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSubstgMissingUnderStrict_ShouldThrowOutlookMsgFormatException()
    {
        var builder = new MsgFixtureBuilder().AddEntryWithoutStream(0x1234001F, 10);

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });

        Assert.IsTrue(ex.Message.Contains("__substg1.0_1234001F", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a storage without a property stream throws.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPropertiesStreamMissing_ShouldThrowOutlookMsgFormatException()
    {
        var builder = new MsgFixtureBuilder().OmitPropertiesStream();

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder);
        });
    }
}
