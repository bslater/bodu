// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMapTests.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgNamedPropertyMapTests
{
    /// <summary>
    /// Verifies that a message without the mapping storage loads the empty mapping.
    /// </summary>
    [TestMethod]
    public void Load_WhenStorageAbsent_ShouldReturnEmptyMap()
    {
        MsgNamedPropertyMap map = Load(MsgFixtureBuilder.CreateMinimal());

        Assert.IsFalse(map.TryGetName(0x8000, out _));
    }

    /// <summary>
    /// Verifies that numeric and string entries load with their property-set GUIDs resolved through all three index
    /// forms.
    /// </summary>
    [TestMethod]
    public void Load_WhenRepresentativeMapping_ShouldResolveBothKinds()
    {
        MsgNamedPropertyMap map = Load(CreateRepresentative());

        Assert.IsTrue(map.TryGetName(0x8000, out MapiNamedProperty numeric));
        Assert.AreEqual(new MapiNamedProperty(CustomSet, 0x00008233u), numeric);

        Assert.IsTrue(map.TryGetName(0x8001, out MapiNamedProperty text));
        Assert.AreEqual(new MapiNamedProperty(PublicStrings, "Keywords"), text);
    }

    /// <summary>
    /// Verifies that a malformed entry stream throws under strict validation and yields the empty mapping under
    /// compatible validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenEntryStreamMisaligned_ShouldReturnEmptyOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), new byte[7], Array.Empty<byte>());

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that an entry referencing a GUID index past the GUID stream is skipped under compatible validation
    /// and throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenGuidIndexOutOfRange_ShouldSkipOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), MsgFixtureBuilder.NameIdEntry(1, 3, isString: false, 0), Array.Empty<byte>());

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a string entry whose offset or length escapes the string stream is skipped under compatible
    /// validation and throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringOffsetOutOfRange_ShouldSkipOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), MsgFixtureBuilder.NameIdEntry(100, 2, isString: true, 0), MsgFixtureBuilder.NameIdString("x"));

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }
}
