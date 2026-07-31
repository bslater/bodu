// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMapTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgNamedPropertyMapTests
{
    /// <summary>
    /// Verifies that the mapping resolves both directions consistently.
    /// </summary>
    [TestMethod]
    public void TryGetId_WhenNameMapped_ShouldRoundTripWithTryGetName()
    {
        MsgNamedPropertyMap map = Load(CreateRepresentative());

        Assert.IsTrue(map.TryGetId(new MapiNamedProperty(PublicStrings, "Keywords"), out ushort id));
        Assert.AreEqual((ushort)0x8001, id);
        Assert.IsTrue(map.TryGetName(id, out MapiNamedProperty name));
        Assert.AreEqual(new MapiNamedProperty(PublicStrings, "Keywords"), name);
    }

    /// <summary>
    /// Verifies that unmapped identities and identifiers report absent.
    /// </summary>
    [TestMethod]
    public void TryGetId_WhenNameUnmapped_ShouldReturnFalse()
    {
        MsgNamedPropertyMap map = Load(CreateRepresentative());

        Assert.IsFalse(map.TryGetId(new MapiNamedProperty(PublicStrings, "Missing"), out _));
        Assert.IsFalse(map.TryGetName(0x9000, out _));
    }
}
