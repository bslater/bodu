// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumeratorContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for the <see cref="IEnumerator{T}" /> surface of a collection.
/// Concrete subclasses override <see cref="Create(TItem[])" /> and <see cref="CreateItem" /> to plug their
/// type into the inherited tests.
/// </summary>
/// <typeparam name="TEnumerable">The enumerable type under test.</typeparam>
/// <typeparam name="TItem">The element type yielded by enumeration.</typeparam>
/// <remarks>
/// <para>
/// Covers the standard enumerator contract: empty-source <c>MoveNext</c> returns <see langword="false" />,
/// <c>MoveNext</c> walks every element, and the enumerator is disposable. Iteration-order assertions are
/// avoided so the base remains usable by ordered and unordered collections alike.
/// </para>
/// </remarks>
public abstract class EnumeratorContractTests<TEnumerable, TItem>
    where TEnumerable : IEnumerable<TItem>
{
    /// <summary>
    /// Creates an enumerable of the type under test seeded with <paramref name="items" />.
    /// </summary>
    /// <param name="items">The items to seed the enumerable with.</param>
    /// <returns>A new enumerable instance containing <paramref name="items" />.</returns>
    protected abstract TEnumerable Create(params TItem[] items);

    /// <summary>
    /// Returns a distinct item suitable for use at position <paramref name="index" />.
    /// </summary>
    /// <param name="index">The zero-based ordinal of the requested item.</param>
    /// <returns>A distinct item for the given ordinal.</returns>
    protected abstract TItem CreateItem(int index);

    /// <summary>
    /// Verifies that an enumerator constructed from an empty source returns <see langword="false" /> on the
    /// first <c>MoveNext</c> call.
    /// </summary>
    [TestMethod]
    public void MoveNext_WhenSourceIsEmpty_ShouldReturnFalse()
    {
        TEnumerable source = Create();

        using IEnumerator<TItem> enumerator = source.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <c>MoveNext</c> walks through every element in the source before returning
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void MoveNext_WhenSourceHasItems_ShouldWalkExactlyCountTimes()
    {
        TItem[] items = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TEnumerable source = Create(items);

        using IEnumerator<TItem> enumerator = source.GetEnumerator();
        int observed = 0;
        while (enumerator.MoveNext())
        {
            observed++;
        }

        Assert.AreEqual(items.Length, observed);
    }

    /// <summary>
    /// Verifies that the enumerator can be disposed without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldNotThrow()
    {
        TEnumerable source = Create(CreateItem(0));

        IEnumerator<TItem> enumerator = source.GetEnumerator();
        enumerator.Dispose();
    }

    /// <summary>
    /// Verifies that the enumerator's <c>Current</c> after a successful <c>MoveNext</c> returns one of the
    /// source items. Implementations that maintain a fixed iteration order may add stronger tests in the
    /// derived class.
    /// </summary>
    [TestMethod]
    public void Current_AfterMoveNext_ShouldReturnAnItemFromTheSource()
    {
        TItem item = CreateItem(0);
        TEnumerable source = Create(item);

        using IEnumerator<TItem> enumerator = source.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(item, enumerator.Current);
    }
}
