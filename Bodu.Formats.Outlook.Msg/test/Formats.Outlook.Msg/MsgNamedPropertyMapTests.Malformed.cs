// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMapTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgNamedPropertyMapTests
{
    /// <summary>
    /// Verifies that a string entry whose offset sits at the top of the unsigned range is skipped under compatible
    /// validation and throws the format exception under strict validation — never an argument exception from a
    /// wrapped bounds check.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringOffsetWrapsUnsigned_ShouldSkipOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), MsgFixtureBuilder.NameIdEntry(0xFFFF_FFFD, 2, isString: true, 0), MsgFixtureBuilder.NameIdString("x"));

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a string entry whose length prefix sits at the top of the unsigned range is skipped under
    /// compatible validation and throws the format exception under strict validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringLengthWrapsUnsigned_ShouldSkipOrThrowByValidationLevel()
    {
        var strings = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(strings, 0xFFFF_FFFC);
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 0), strings);

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that an entry whose property index would wrap the mapped identifier out of the named range — onto
    /// <c>PidTagSubject</c> here — is rejected rather than letting a named property impersonate a well-known tag.
    /// </summary>
    [TestMethod]
    public void Load_WhenPropertyIndexWrapsIntoWellKnownRange_ShouldSkipOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(CustomSet.ToByteArray(), MsgFixtureBuilder.NameIdEntry(0x00008233u, 3, isString: false, 0x8037), Array.Empty<byte>());

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(MapiPropertyIds.Subject, out _), "A named entry must never map onto a well-known identifier.");
        Assert.IsFalse(tolerant.TryGetId(new MapiNamedProperty(CustomSet, 0x00008233u), out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a string entry naming a whitespace-only property is treated as malformed content: skipped under
    /// compatible validation and the format exception under strict — never the identity type's argument exception.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringNameIsWhitespace_ShouldSkipOrThrowByValidationLevel()
    {
        MsgFixtureBuilder builder = MsgFixtureBuilder.CreateMinimal()
            .WithNameId(Array.Empty<byte>(), MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 0), MsgFixtureBuilder.NameIdString("  "));

        MsgNamedPropertyMap tolerant = Load(builder);
        Assert.IsFalse(tolerant.TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Load(builder, CompoundValidationLevel.Strict);
        });
    }
}
