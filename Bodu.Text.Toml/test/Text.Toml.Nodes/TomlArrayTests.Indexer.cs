// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlArray" /> indexer, including an assignment that is rejected.
/// </summary>
public partial class TomlArrayTests
{
    /// <summary>
    /// Verifies that assigning to an index outside the array throws before the value is attached, leaving it without a
    /// parent.
    /// </summary>
    /// <param name="index">The out-of-range index: negative, or the count.</param>
    [TestMethod]
    [DataRow(-1, DisplayName = "negative")]
    [DataRow(1, DisplayName = "the count")]
    public void Indexer_WhenSetOutOfRange_ShouldLeaveValueWithoutParent(int index)
    {
        var array = new TomlArray(TomlValue.Create(1L));
        var value = TomlValue.Create(2L);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array[index] = value;
        });

        Assert.IsNull(value.Parent);
    }
}
