// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNamedPropertyMapTests.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Outlook.Pst;

public partial class PstNamedPropertyMapTests
{
    /// <summary>
    /// Verifies that a string entry whose offset sits at the top of the unsigned range is skipped under the tolerant
    /// levels and throws the format exception under strict validation — never an argument exception from a wrapped
    /// bounds check.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringOffsetWrapsUnsigned_ShouldSkipOrThrowByValidationLevel()
    {
        byte[] entries = Entry(0xFFFF_FFFD, 2, isString: true, 0);
        byte[] strings = [4, 0, 0, 0, (byte)'x', 0, (byte)'y', 0];

        Assert.IsFalse(Load([], entries, strings, strict: false).TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = Load([], entries, strings, strict: true);
        });
    }

    /// <summary>
    /// Verifies that a string entry whose length prefix sits at the top of the unsigned range is skipped under the
    /// tolerant levels and throws the format exception under strict validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenStringLengthWrapsUnsigned_ShouldSkipOrThrowByValidationLevel()
    {
        byte[] entries = Entry(0, 2, isString: true, 0);
        var strings = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(strings, 0xFFFF_FFFC);

        Assert.IsFalse(Load([], entries, strings, strict: false).TryGetName(0x8000, out _));

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = Load([], entries, strings, strict: true);
        });
    }

    /// <summary>
    /// Verifies that an entry whose property index would wrap the mapped identifier out of the named range is
    /// rejected rather than letting a named property impersonate a well-known tag.
    /// </summary>
    [TestMethod]
    public void Load_WhenPropertyIndexWrapsIntoWellKnownRange_ShouldSkipOrThrowByValidationLevel()
    {
        byte[] entries = Entry(0x00008233u, 3, isString: false, 0x8037);
        byte[] guids = CustomSet.ToByteArray();

        PstNamedPropertyMap tolerant = Load(guids, entries, [], strict: false);
        Assert.IsFalse(tolerant.TryGetName(MapiPropertyIds.Subject, out _), "A named entry must never map onto a well-known identifier.");
        Assert.IsFalse(tolerant.TryGetId(new MapiNamedProperty(CustomSet, 0x00008233u), out _));

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = Load(guids, entries, [], strict: true);
        });
    }
}
