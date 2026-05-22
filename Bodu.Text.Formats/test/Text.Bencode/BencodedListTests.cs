// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedListTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="BencodedList" />. Partial files split coverage by member or subject.
/// </summary>
[TestClass]
public sealed partial class BencodedListTests
{

    /// <summary>
    /// Verifies that the constructor accepts an empty enumerable and produces an empty list with
    /// <see cref="BencodedList.Count" /> equal to zero.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEnumerableIsEmpty_ShouldProduceEmptyList()
    {
        BencodedList list = new(Array.Empty<BencodedValue>());

        Assert.AreEqual(0, list.Count);
    }
    /// <summary>
    /// Verifies that the constructor accepts a non-empty enumerable and exposes the items via
    /// <see cref="BencodedList.Items" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEnumerableProvided_ShouldExposeItems()
    {
        BencodedList list = new(new BencodedValue[]
        {
            BencodedString.FromUtf8("spam"),
            new BencodedInteger(42),
        });

        Assert.AreEqual(2, list.Count);
        Assert.IsInstanceOfType<BencodedString>(list.Items[0]);
        Assert.IsInstanceOfType<BencodedInteger>(list.Items[1]);
    }

    /// <summary>
    /// Verifies that the indexer returns the same instance as <see cref="BencodedList.Items" /> at the
    /// corresponding offset.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAccessed_ShouldReturnSameInstanceAsItems()
    {
        BencodedInteger expected = new(7);
        BencodedList list = new(new BencodedValue[] { expected });

        Assert.AreSame(expected, list[0]);
        Assert.AreSame(list.Items[0], list[0]);
    }

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is negative,
    /// rather than surfacing an <see cref="IndexOutOfRangeException" /> from the backing array.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        BencodedList list = new(new BencodedValue[] { new BencodedInteger(1) });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list[-1];
        });
    }

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is greater than
    /// or equal to <see cref="BencodedList.Count" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsBeyondCount_ShouldThrowArgumentOutOfRangeException()
    {
        BencodedList list = new(new BencodedValue[] { new BencodedInteger(1) });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list[1];
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodedValue.Kind" /> returns <see cref="BencodedValueKind.List" />.
    /// </summary>
    [TestMethod]
    public void Kind_ShouldReturnList()
    {
        BencodedList list = new(Array.Empty<BencodedValue>());

        Assert.AreEqual(BencodedValueKind.List, list.Kind);
    }

}
