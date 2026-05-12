// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.MultipleInstances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that multiple collection instances maintain independent state and contents.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldMaintainIndependentState()
    {
        TCollection c1 = CreateCollection(3);
        TCollection c2 = CreateCollection(3);

        AddToTail(c1, 1);
        AddToTail(c1, 2);
        AddToTail(c2, 10);
        AddToTail(c2, 20);

        CollectionAssert.AreEqual(new[] { 1, 2 }, ToArray(c1));
        CollectionAssert.AreEqual(new[] { 10, 20 }, ToArray(c2));
    }

    /// <summary>
    /// Verifies that mutating one collection does not invalidate enumerators of another.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldHaveIsolatedVersionTokens()
    {
        TCollection c1 = CreateCollection(3);
        TCollection c2 = CreateCollection(3);

        AddToTail(c1, 1);
        AddToTail(c2, 10);

        IEnumerator<int> enumerator2 = c2.GetEnumerator();
        AddToTail(c1, 2);

        Assert.IsTrue(enumerator2.MoveNext());
        Assert.AreEqual(10, enumerator2.Current);
        Assert.IsFalse(enumerator2.MoveNext());
    }
}
