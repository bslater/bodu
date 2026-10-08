// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObjectTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeObject" /> constructors, including a construction that fails part-way through its
/// items.
/// </summary>
public partial class BencodeObjectTests
{
    /// <summary>
    /// Verifies that a construction that fails on an item whose key repeats an earlier one leaves every value it was
    /// given free to be added elsewhere: the values already attached are released, and the rejected value is never
    /// attached.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemRepeatsAKey_ShouldLeaveEveryValueFreeToAddElsewhere()
    {
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create(2L);
        var repeated = BencodeValue.Create(3L);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodeObject(
            [
                new KeyValuePair<string, BencodeNode?>("a", first),
                new KeyValuePair<string, BencodeNode?>("b", second),
                new KeyValuePair<string, BencodeNode?>("a", repeated),
            ]);
        });

        var other = new BencodeObject { { "a", first }, { "b", second }, { "c", repeated } };

        Assert.AreEqual(3, other.Count);
    }

    /// <summary>
    /// Verifies that a construction that fails on a value that belongs to another container leaves every value with the
    /// parent it had: the values already attached are released, and the failing value stays with its container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemBelongsToAnotherContainer_ShouldLeaveEveryValueWithItsParent()
    {
        var free = BencodeValue.Create(1L);
        var owned = BencodeValue.Create(2L);
        var owner = new BencodeArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new BencodeObject([new KeyValuePair<string, BencodeNode?>("a", free), new KeyValuePair<string, BencodeNode?>("b", owned)]);
        });

        Assert.IsNull(free.Parent, "The parent of the value attached before the failure.");
        Assert.AreSame(owner, owned.Parent, "The parent of the value that belongs to another container.");
    }

    /// <summary>
    /// Verifies that a construction that fails on a <see langword="null" /> key releases the values already attached.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemHasNullKey_ShouldReleaseTheValuesAlreadyAttached()
    {
        var free = BencodeValue.Create(1L);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodeObject([new KeyValuePair<string, BencodeNode?>("a", free), new KeyValuePair<string, BencodeNode?>(null!, BencodeValue.Create(2L))]);
        });

        Assert.IsNull(free.Parent);
    }
}
