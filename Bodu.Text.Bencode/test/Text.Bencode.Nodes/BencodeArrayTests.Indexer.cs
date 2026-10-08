// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeArrayTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeArray" /> indexer, including an assignment that is rejected.
/// </summary>
public partial class BencodeArrayTests
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
        var array = new BencodeArray(BencodeValue.Create(1L));
        var value = BencodeValue.Create(2L);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array[index] = value;
        });

        Assert.IsNull(value.Parent);
    }
}
