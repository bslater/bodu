// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeArrayTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies <see cref="BencodeArray.Add(BencodeNode?)" />, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class BencodeArrayTests
{
    /// <summary>
    /// Verifies that adding a node a second time to the array that already holds it throws
    /// <see cref="InvalidOperationException" /> and leaves a single occurrence attached to the array.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAlreadyAnElement_ShouldThrowInvalidOperationException()
    {
        var value = BencodeValue.Create(1L);
        var array = new BencodeArray();
        array.Add(value);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(value);
        });

        Assert.AreEqual(1, array.Count, "The array's count after the rejected add.");
        Assert.AreSame(array, value.Parent, "The parent of the element.");
    }
}
