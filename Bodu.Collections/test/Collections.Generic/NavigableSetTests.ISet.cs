// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.ISet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that each of the four set-mutation operations produces exactly the contents predicted by a
    /// <see cref="HashSet{T}" /> oracle over seeded random operands.
    /// </summary>
    [TestMethod]
    public void SetOperations_WhenAppliedToRandomOperands_ShouldMatchHashSetOracle()
    {
        var random = new Random(20260705);

        for (int trial = 0; trial < 20; trial++)
        {
            int[] left = Enumerable.Range(0, 40).Where(_ => random.Next(2) == 0).ToArray();
            int[] right = Enumerable.Range(0, 40).Where(_ => random.Next(2) == 0).ToArray();

            foreach (string operation in new[] { "union", "intersect", "except", "symmetricExcept" })
            {
                var sut = new NavigableSet<int>(left);
                var oracle = new HashSet<int>(left);

                switch (operation)
                {
                    case "union":
                        sut.UnionWith(right);
                        oracle.UnionWith(right);
                        break;

                    case "intersect":
                        sut.IntersectWith(right);
                        oracle.IntersectWith(right);
                        break;

                    case "except":
                        sut.ExceptWith(right);
                        oracle.ExceptWith(right);
                        break;

                    default:
                        sut.SymmetricExceptWith(right);
                        oracle.SymmetricExceptWith(right);
                        break;
                }

                int[] expected = oracle.OrderBy(x => x).ToArray();
                CollectionAssert.AreEqual(expected, sut.ToList(), $"{operation} disagrees with the oracle in trial {trial}.");
                Assert.AreEqual(expected.Length, sut.Count);
            }
        }
    }

    /// <summary>
    /// Verifies that each of the six set-relation predicates agrees with the <see cref="HashSet{T}" /> oracle over
    /// seeded random operands.
    /// </summary>
    [TestMethod]
    public void SetRelations_WhenAppliedToRandomOperands_ShouldMatchHashSetOracle()
    {
        var random = new Random(19570301);

        for (int trial = 0; trial < 40; trial++)
        {
            int[] left = Enumerable.Range(0, 12).Where(_ => random.Next(2) == 0).ToArray();
            int[] right = Enumerable.Range(0, 12).Where(_ => random.Next(2) == 0).ToArray();

            var sut = new NavigableSet<int>(left);
            var oracle = new HashSet<int>(left);

            Assert.AreEqual(oracle.IsSubsetOf(right), sut.IsSubsetOf(right), $"IsSubsetOf disagrees in trial {trial}.");
            Assert.AreEqual(oracle.IsSupersetOf(right), sut.IsSupersetOf(right), $"IsSupersetOf disagrees in trial {trial}.");
            Assert.AreEqual(oracle.IsProperSubsetOf(right), sut.IsProperSubsetOf(right), $"IsProperSubsetOf disagrees in trial {trial}.");
            Assert.AreEqual(oracle.IsProperSupersetOf(right), sut.IsProperSupersetOf(right), $"IsProperSupersetOf disagrees in trial {trial}.");
            Assert.AreEqual(oracle.Overlaps(right), sut.Overlaps(right), $"Overlaps disagrees in trial {trial}.");
            Assert.AreEqual(oracle.SetEquals(right), sut.SetEquals(right), $"SetEquals disagrees in trial {trial}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.UnionWith" /> with the set itself is a no-op.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsSelf_ShouldLeaveSetUnchanged()
    {
        var sut = CreateSet(1, 2, 3);

        sut.UnionWith(sut);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.IntersectWith" /> with the set itself is a no-op.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherIsSelf_ShouldLeaveSetUnchanged()
    {
        var sut = CreateSet(1, 2, 3);

        sut.IntersectWith(sut);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.ExceptWith" /> and
    /// <see cref="NavigableSet{T}.SymmetricExceptWith" /> with the set itself empty the set.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherIsSelf_ShouldEmptySet()
    {
        var except = CreateSet(1, 2, 3);
        var symmetric = CreateSet(1, 2, 3);

        except.ExceptWith(except);
        symmetric.SymmetricExceptWith(symmetric);

        Assert.AreEqual(0, except.Count);
        Assert.AreEqual(0, symmetric.Count);
    }

    /// <summary>
    /// Verifies that the six set-relation predicates report the reflexive answers when the argument is the set
    /// itself.
    /// </summary>
    [TestMethod]
    public void SetRelations_WhenOtherIsSelf_ShouldReportReflexiveRelations()
    {
        var sut = CreateSet(1, 2, 3);

        Assert.IsTrue(sut.SetEquals(sut));
        Assert.IsTrue(sut.IsSubsetOf(sut));
        Assert.IsTrue(sut.IsSupersetOf(sut));
        Assert.IsFalse(sut.IsProperSubsetOf(sut));
        Assert.IsFalse(sut.IsProperSupersetOf(sut));
        Assert.IsTrue(sut.Overlaps(sut));
    }

    /// <summary>
    /// Verifies that comparer-equal duplicates in the argument are ignored by the set-relation predicates, matching
    /// <see cref="HashSet{T}" /> semantics.
    /// </summary>
    [TestMethod]
    public void SetRelations_WhenOtherContainsDuplicates_ShouldIgnoreDuplicates()
    {
        var sut = CreateSet(1, 2, 3);

        Assert.IsTrue(sut.SetEquals(new[] { 3, 1, 2, 2, 1, 3 }));
        Assert.IsTrue(sut.IsSubsetOf(new[] { 1, 1, 2, 2, 3, 3 }));
        Assert.IsFalse(sut.IsProperSubsetOf(new[] { 1, 1, 2, 2, 3, 3 }));
        Assert.IsTrue(sut.IsProperSubsetOf(new[] { 1, 1, 2, 2, 3, 3, 4, 4 }));
        Assert.IsTrue(sut.IsProperSupersetOf(new[] { 1, 1, 2, 2 }));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.SymmetricExceptWith" /> toggles membership once per distinct element
    /// of the argument, ignoring duplicates.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherContainsDuplicates_ShouldToggleOncePerDistinctElement()
    {
        var sut = CreateSet(1, 2, 3);

        sut.SymmetricExceptWith(new[] { 2, 2, 4, 4 });

        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that the projection-based set operations reject an argument containing a <see langword="null" />
    /// element with <see cref="ArgumentException" />, matching the bulk-load constructor's null policy.
    /// </summary>
    [TestMethod]
    public void SetOperations_WhenOtherContainsNullElement_ShouldThrowArgumentException()
    {
        var sut = new NavigableSet<string>();
        sut.Add("a");

        Assert.ThrowsExactly<ArgumentException>(() => { _ = sut.SetEquals(new[] { "a", null! }); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = sut.IsSubsetOf(new[] { "a", null! }); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = sut.IsProperSubsetOf(new[] { "a", null! }); });
        Assert.ThrowsExactly<ArgumentException>(() => { _ = sut.IsProperSupersetOf(new[] { "a", null! }); });
        Assert.ThrowsExactly<ArgumentException>(() => { sut.IntersectWith(new[] { "a", null! }); });
        Assert.ThrowsExactly<ArgumentException>(() => { sut.SymmetricExceptWith(new[] { "a", null! }); });
    }

    /// <summary>
    /// Verifies that the set algebra follows the receiver's comparer when the argument carries comparer-equal
    /// spellings.
    /// </summary>
    [TestMethod]
    public void SetOperations_WhenReceiverUsesCustomComparer_ShouldApplyReceiverComparer()
    {
        var sut = new NavigableSet<string>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha");
        sut.Add("Beta");

        sut.ExceptWith(new[] { "ALPHA" });

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("Beta", sut.Min);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Add" /> routes through the set-semantics add, ignoring duplicates.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsDuplicate_ShouldNotIncreaseCount()
    {
        ICollection<int> sut = CreateSet(1, 2);

        sut.Add(2);

        Assert.AreEqual(2, sut.Count);
    }

    /// <summary>
    /// Verifies that every set-algebra member throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void SetOperations_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSet(1);

        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.UnionWith(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.IntersectWith(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.ExceptWith(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.SymmetricExceptWith(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.IsSubsetOf(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.IsSupersetOf(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.IsProperSubsetOf(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.IsProperSupersetOf(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.Overlaps(null!), "other");
        ThrowsExactlyWithParamName<ArgumentNullException>(() => sut.SetEquals(null!), "other");
    }
}
