// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeIdTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstNodeId" />, the 32-bit node identifier.
/// </summary>
[TestClass]
public partial class PstNodeIdTests
{
    /// <summary>
    /// Verifies that the raw constructor splits the identifier into its type and index bits.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawValue_ShouldSplitTypeAndIndex()
    {
        var id = new PstNodeId(0x00200024);

        Assert.AreEqual(0x00200024u, id.Value);
        Assert.AreEqual(PstNodeType.NormalMessage, id.Type);
        Assert.AreEqual(0x00200024u >> 5, id.Index);
    }

    /// <summary>
    /// Verifies that the typed constructor packs the type into the five low bits and the index above them, masking
    /// stray type bits.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTypeAndIndex_ShouldPackValue()
    {
        var id = new PstNodeId(PstNodeType.NormalFolder, 9);

        Assert.AreEqual((9u << 5) | 0x02, id.Value);
        Assert.AreEqual(PstNodeType.NormalFolder, id.Type);
        Assert.AreEqual(9u, id.Index);
    }

    /// <summary>
    /// Verifies the fixed values of the well-known identifiers (MS-PST §2.4.1).
    /// </summary>
    [TestMethod]
    public void WellKnowns_WhenInspected_ShouldCarrySpecValues()
    {
        Assert.AreEqual(0x21u, PstNodeId.MessageStore.Value);
        Assert.AreEqual(0x61u, PstNodeId.NameToIdMap.Value);
        Assert.AreEqual(0x122u, PstNodeId.RootFolder.Value);
        Assert.AreEqual(PstNodeType.Internal, PstNodeId.MessageStore.Type);
        Assert.AreEqual(PstNodeType.NormalFolder, PstNodeId.RootFolder.Type);
    }
}
