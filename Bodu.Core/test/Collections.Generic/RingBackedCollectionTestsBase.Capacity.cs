// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="GetCapacity(TCollection)"/> is at least the requested initial capacity, across
    /// a range of sizes. Fixed-capacity types report exactly the requested value; growable types may report more.
    /// </summary>
    /// <param name="capacity">The capacity hint passed to the constructor.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(7)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(1024)]
    public void Capacity_WhenCustomCapacityProvided_ShouldBeAtLeastRequested(int capacity)
    {
        TCollection collection = CreateCollection(capacity);

        if (ReportsExactCapacity)
            Assert.AreEqual(capacity, GetCapacity(collection));
        else
            Assert.IsGreaterThanOrEqualTo(capacity, GetCapacity(collection));
    }

    /// <summary>
    /// Verifies that <see cref="GetCapacity(TCollection)"/> is at least <see cref="GetCount(TCollection)"/>
    /// after a series of inserts.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenItemsAdded_ShouldBeAtLeastCount()
    {
        TCollection collection = CreateCollection(4);
        for (var i = 0; i < 4; i++)
            AddToTail(collection, i);

        Assert.IsGreaterThanOrEqualTo(GetCount(collection), GetCapacity(collection));
    }

    /// <summary>
    /// Verifies that <see cref="GetCapacity(TCollection)"/> remains stable across adds and removes that do
    /// not exceed the initial capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenItemsAddedAndRemovedWithinCapacity_ShouldRemainConstant()
    {
        TCollection collection = CreateCollection(5);
        var capacityBefore = GetCapacity(collection);

        AddToTail(collection, 1);
        AddToTail(collection, 2);
        _ = RemoveFromHead(collection);

        Assert.AreEqual(capacityBefore, GetCapacity(collection));
    }

}
