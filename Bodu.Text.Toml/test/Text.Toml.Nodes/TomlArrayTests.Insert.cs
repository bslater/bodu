// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.Insert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies <see cref="TomlArray.Insert(int, TomlNode?)" />, including an insert that is rejected.
/// </summary>
public partial class TomlArrayTests
{
    /// <summary>
    /// Verifies that an insert rejected for an index outside the array leaves the item without a parent, free to be
    /// added elsewhere, and the array unchanged.
    /// </summary>
    /// <param name="index">The out-of-range index: negative, or one past the end.</param>
    [TestMethod]
    [DataRow(-1, DisplayName = "negative")]
    [DataRow(2, DisplayName = "one past the end")]
    public void Insert_WhenIndexIsOutOfRange_ShouldLeaveItemFreeToAddElsewhere(int index)
    {
        var array = new TomlArray(TomlValue.Create(1L));
        var item = TomlValue.Create(2L);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Insert(index, item);
        });

        Assert.IsNull(item.Parent, "The item's parent after the rejected insert.");
        Assert.AreEqual(1, array.Count, "The array's count after the rejected insert.");
        var other = new TomlArray();
        other.Add(item);
        Assert.AreSame(other, item.Parent, "The item's parent once added elsewhere.");
    }
}
