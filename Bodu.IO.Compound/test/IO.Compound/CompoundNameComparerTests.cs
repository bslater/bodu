// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundNameComparerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the compound-file name relationship: length first, then invariant uppercase per code unit.
/// </summary>
/// <remarks>
/// The ordering is the format's, not the CLR's - a directory red-black tree built with any other comparison produces
/// a container other readers cannot navigate. Equality drops the length-first rule (names of different lengths are
/// simply unequal) but keeps the same per-code-unit uppercase comparison, so the two views agree on which names
/// denote the same entry.
/// </remarks>
[TestClass]
public class CompoundNameComparerTests
{
    /// <summary>
    /// Verifies that a shorter name always orders before a longer one, whatever the characters are - the format
    /// compares length before content.
    /// </summary>
    [TestMethod]
    public void Compare_WhenLengthsDiffer_ShouldOrderShorterFirst()
    {
        Assert.IsLessThan(0, CompoundNameComparer.Instance.Compare("ab", "abc"));
        Assert.IsGreaterThan(0, CompoundNameComparer.Instance.Compare("abc", "ab"));

        // "z" is longer-last even though it sorts after "aaa" under an ordinary string comparison.
        Assert.IsLessThan(0, CompoundNameComparer.Instance.Compare("z", "aaa"));
    }

    /// <summary>
    /// Verifies that equal-length names are ordered by their uppercased code units, so case does not affect the
    /// ordering.
    /// </summary>
    [TestMethod]
    public void Compare_WhenLengthsMatch_ShouldOrderByUppercasedCodeUnits()
    {
        Assert.AreEqual(0, CompoundNameComparer.Instance.Compare("abc", "ABC"));
        Assert.IsLessThan(0, CompoundNameComparer.Instance.Compare("abc", "abd"));
        Assert.IsGreaterThan(0, CompoundNameComparer.Instance.Compare("ABD", "abc"));
    }

    /// <summary>
    /// Verifies that the same reference compares equal without inspecting its content.
    /// </summary>
    [TestMethod]
    public void Compare_WhenSameReference_ShouldReturnZero()
    {
        const string Name = "Workbook";

        Assert.AreEqual(0, CompoundNameComparer.Instance.Compare(Name, Name));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> name orders before a non-<see langword="null" /> one, and that two
    /// <see langword="null" /> names compare equal.
    /// </summary>
    [TestMethod]
    public void Compare_WhenEitherNameIsNull_ShouldOrderNullFirst()
    {
        Assert.AreEqual(0, CompoundNameComparer.Instance.Compare(null, null));
        Assert.IsLessThan(0, CompoundNameComparer.Instance.Compare(null, "a"));
        Assert.IsGreaterThan(0, CompoundNameComparer.Instance.Compare("a", null));
    }

    /// <summary>
    /// Verifies that equality ignores case but not length, and that equal names share a hash bucket - without which a
    /// dictionary keyed by this comparer would miss case-variant lookups.
    /// </summary>
    [TestMethod]
    public void Equals_WhenNamesDifferOnlyByCase_ShouldBeEqualAndShareHashCode()
    {
        Assert.IsTrue(CompoundNameComparer.Instance.Equals("Workbook", "WORKBOOK"));
        Assert.AreEqual(
            CompoundNameComparer.Instance.GetHashCode("Workbook"),
            CompoundNameComparer.Instance.GetHashCode("WORKBOOK"));

        Assert.IsFalse(CompoundNameComparer.Instance.Equals("Workbook", "Workbook2"));
    }

    /// <summary>
    /// Verifies that <see langword="null" /> is equal only to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEitherNameIsNull_ShouldOnlyMatchAnotherNull()
    {
        Assert.IsTrue(CompoundNameComparer.Instance.Equals(null, null));
        Assert.IsFalse(CompoundNameComparer.Instance.Equals(null, "a"));
        Assert.IsFalse(CompoundNameComparer.Instance.Equals("a", null));
    }

    /// <summary>
    /// Verifies that the comparer is exposed as a single shared instance, since it holds no state.
    /// </summary>
    [TestMethod]
    public void Instance_ShouldBeShared()
    {
        Assert.AreSame(CompoundNameComparer.Instance, CompoundNameComparer.Instance);
    }
}
