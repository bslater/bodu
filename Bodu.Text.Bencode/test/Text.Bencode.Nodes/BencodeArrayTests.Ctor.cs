// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeArrayTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeArray" /> constructors, including a construction that fails part-way through its
/// items.
/// </summary>
public partial class BencodeArrayTests
{
    /// <summary>
    /// Verifies that a construction that fails on an item that belongs to another container leaves every item with the
    /// parent it had: the items already attached are released, and the failing item stays with its container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemBelongsToAnotherContainer_ShouldLeaveEveryItemWithItsParent()
    {
        var free = BencodeValue.Create(1L);
        var owned = BencodeValue.Create(2L);
        var owner = new BencodeArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new BencodeArray(free, owned);
        });

        Assert.IsNull(free.Parent, "The parent of the item attached before the failure.");
        Assert.AreSame(owner, owned.Parent, "The parent of the item that belongs to another container.");
    }
}
