// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObjectTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlObject" /> constructors, including a construction that fails part-way through its items.
/// </summary>
public partial class TomlObjectTests
{
    /// <summary>
    /// Verifies that a construction that fails on an item whose key repeats an earlier one leaves every value it was
    /// given free to be added elsewhere: the values already attached are released, and the rejected value is never
    /// attached.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemRepeatsAKey_ShouldLeaveEveryValueFreeToAddElsewhere()
    {
        var first = TomlValue.Create(1L);
        var second = TomlValue.Create(2L);
        var repeated = TomlValue.Create(3L);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TomlObject(
            [
                new KeyValuePair<string, TomlNode?>("a", first),
                new KeyValuePair<string, TomlNode?>("b", second),
                new KeyValuePair<string, TomlNode?>("a", repeated),
            ]);
        });

        var other = new TomlObject { { "a", first }, { "b", second }, { "c", repeated } };

        Assert.AreEqual(3, other.Count);
    }

    /// <summary>
    /// Verifies that a construction that fails on a value that belongs to another container leaves every value with the
    /// parent it had: the values already attached are released, and the failing value stays with its container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenALaterItemBelongsToAnotherContainer_ShouldLeaveEveryValueWithItsParent()
    {
        var free = TomlValue.Create(1L);
        var owned = TomlValue.Create(2L);
        var owner = new TomlArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new TomlObject([new KeyValuePair<string, TomlNode?>("a", free), new KeyValuePair<string, TomlNode?>("b", owned)]);
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
        var free = TomlValue.Create(1L);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TomlObject([new KeyValuePair<string, TomlNode?>("a", free), new KeyValuePair<string, TomlNode?>(null!, TomlValue.Create(2L))]);
        });

        Assert.IsNull(free.Parent);
    }
}
