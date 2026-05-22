// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="AddToTail(TCollection, int)"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAddToTailCalledDuringIteration_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        AddToTail(collection, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="Clear(TCollection)"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearCalledDuringIteration_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Clear(collection);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }
    /// <summary>
    /// Verifies that the generic <see cref="IEnumerator{T}"/> iterates elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCollectionHasItems_ShouldIterateInHeadToTailOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        var collected = new List<int>();
        while (enumerator.MoveNext())
            collected.Add(enumerator.Current);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collected);
    }

    /// <summary>
    /// Verifies that <c>Current</c> throws after iteration is exhausted.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(2);
        AddToTail(collection, 1);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        while (enumerator.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    /// <summary>
    /// Verifies that <c>Current</c> throws before <c>MoveNext</c> is called.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        IEnumerator<int> enumerator = collection.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    /// <summary>
    /// Verifies that the enumerator throws on <c>Reset</c> after mutation.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenMutatedThenReset_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        AddToTail(collection, 2);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.Reset());
    }

    /// <summary>
    /// Verifies that <see cref="RemoveFromHead(TCollection)"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenRemoveFromHeadCalledDuringIteration_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = RemoveFromHead(collection);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <c>Reset</c> on an unmodified enumerator allows iteration to restart from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalledWithoutModification_ShouldRestartIteration()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 10);
        AddToTail(collection, 20);
        AddToTail(collection, 30);

        IEnumerator<int> enumerator = collection.GetEnumerator();
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
            firstPass.Add(enumerator.Current);

        enumerator.Reset();

        var secondPass = new List<int>();
        while (enumerator.MoveNext())
            secondPass.Add(enumerator.Current);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, firstPass);
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

}
