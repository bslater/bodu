// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SetContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Reusable behavioural contract test base for types that expose the <see cref="ISet{T}" /> surface.
/// Concrete subclasses override <see cref="CreateSet(TItem[])" /> and <see cref="CreateItem" /> to plug
/// their type into the inherited contract tests.
/// </summary>
/// <typeparam name="TSet">The set type under test.</typeparam>
/// <typeparam name="TItem">The element type held by the set.</typeparam>
/// <remarks>
/// <para>
/// The base focuses on the set-specific operations beyond <see cref="ICollection{T}" />: <c>Add</c> return
/// semantics, the four set-mutation operations (<c>UnionWith</c>, <c>IntersectWith</c>, <c>ExceptWith</c>,
/// <c>SymmetricExceptWith</c>), and the six set-relation predicates (<c>IsSubsetOf</c>, <c>IsSupersetOf</c>,
/// <c>IsProperSubsetOf</c>, <c>IsProperSupersetOf</c>, <c>Overlaps</c>, <c>SetEquals</c>).
/// </para>
/// </remarks>
public abstract class SetContractTests<TSet, TItem>
    where TSet : ISet<TItem>
{
    /// <summary>
    /// Creates a set of the type under test seeded with <paramref name="items" />. Duplicates in the input
    /// must be collapsed by the implementation per the set semantics.
    /// </summary>
    /// <param name="items">The items to seed the set with.</param>
    /// <returns>A new set instance containing the distinct elements of <paramref name="items" />.</returns>
    protected abstract TSet CreateSet(params TItem[] items);

    /// <summary>
    /// Returns a distinct item suitable for use at position <paramref name="index" />. Implementations
    /// must return values whose equality / hash semantics align with the equality comparer the set uses.
    /// </summary>
    /// <param name="index">The zero-based ordinal of the requested item.</param>
    /// <returns>A distinct item for the given ordinal.</returns>
    protected abstract TItem CreateItem(int index);

    /// <summary>
    /// Verifies that <see cref="ISet{T}.Add" /> returns <see langword="true" /> for an item not yet present.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNew_ShouldReturnTrue()
    {
        TSet set = CreateSet();

        bool added = set.Add(CreateItem(0));

        Assert.IsTrue(added);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.Add" /> returns <see langword="false" /> for a duplicate and does
    /// not increase the count.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldReturnFalseAndNotIncreaseCount()
    {
        TItem item = CreateItem(0);
        TSet set = CreateSet(item);
        int countBefore = set.Count;

        bool added = set.Add(item);

        Assert.IsFalse(added);
        Assert.HasCount(countBefore, set);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.UnionWith" /> adds every element from <paramref name="other" /> that
    /// is not already present.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenInvoked_ShouldContainEveryElementFromBothSets()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b);

        set.UnionWith([b, c]);

        Assert.Contains(a, set);
        Assert.Contains(b, set);
        Assert.Contains(c, set);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.IntersectWith" /> reduces the set to only those elements that
    /// appear in both source sets.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenInvoked_ShouldKeepOnlyCommonElements()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b, c);

        set.IntersectWith([b, c]);

        Assert.DoesNotContain(a, set);
        Assert.Contains(b, set);
        Assert.Contains(c, set);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.ExceptWith" /> removes every element that appears in
    /// <paramref name="other" />.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenInvoked_ShouldRemoveAllElementsInOther()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b, c);

        set.ExceptWith([b, c]);

        Assert.Contains(a, set);
        Assert.DoesNotContain(b, set);
        Assert.DoesNotContain(c, set);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.SymmetricExceptWith" /> keeps elements that appear in exactly one
    /// of the two input collections.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenInvoked_ShouldKeepElementsInExactlyOneCollection()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b);

        set.SymmetricExceptWith([b, c]);

        Assert.Contains(a, set);
        Assert.DoesNotContain(b, set);
        Assert.Contains(c, set);
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.IsSubsetOf" /> reports <see langword="true" /> when every element
    /// of the receiver is contained in <paramref name="other" />.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenReceiverIsContainedInOther_ShouldReturnTrue()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet subset = CreateSet(a, b);

        Assert.IsTrue(subset.IsSubsetOf([a, b, c]));
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.IsSupersetOf" /> reports <see langword="true" /> when every element
    /// of <paramref name="other" /> is contained in the receiver.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenReceiverContainsOther_ShouldReturnTrue()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet superset = CreateSet(a, b, c);

        Assert.IsTrue(superset.IsSupersetOf([a, b]));
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.Overlaps" /> reports <see langword="true" /> when the receiver and
    /// <paramref name="other" /> share at least one element.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenAnyElementIsShared_ShouldReturnTrue()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b);

        Assert.IsTrue(set.Overlaps([b, c]));
    }

    /// <summary>
    /// Verifies that <see cref="ISet{T}.SetEquals" /> reports <see langword="true" /> for equal sets
    /// irrespective of insertion order.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenContentsMatch_ShouldReturnTrue()
    {
        TItem a = CreateItem(0);
        TItem b = CreateItem(1);
        TItem c = CreateItem(2);
        TSet set = CreateSet(a, b, c);

        Assert.IsTrue(set.SetEquals([c, a, b]));
    }
}
