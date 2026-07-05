// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Compare(string?, string?)" /> treats two
    /// <see langword="null" /> references as equal.
    /// </summary>
    [TestMethod]
    public void Compare_WhenBothOperandsAreNull_ShouldReturnZero() =>
        Assert.AreEqual(0, NaturalStringComparer.Ordinal.Compare(null, null));

    /// <summary>
    /// Verifies that <see langword="null" /> sorts before any non-<see langword="null" /> string, including the
    /// empty string, per the BCL comparer convention.
    /// </summary>
    [TestMethod]
    public void Compare_WhenOneOperandIsNull_ShouldSortNullFirst()
    {
        Assert.IsTrue(NaturalStringComparer.Ordinal.Compare(null, string.Empty) < 0);
        Assert.IsTrue(NaturalStringComparer.Ordinal.Compare(string.Empty, null) > 0);
        Assert.IsTrue(NaturalStringComparer.Ordinal.Compare(null, "file1") < 0);
        Assert.IsTrue(NaturalStringComparer.Ordinal.Compare("file1", null) > 0);
    }

    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Equals(string?, string?)" /> reports two
    /// <see langword="null" /> references as equal and a <see langword="null" /> / non-<see langword="null" /> pair
    /// as unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOperandsIncludeNull_ShouldFollowComparerConvention()
    {
        Assert.IsTrue(NaturalStringComparer.Ordinal.Equals(null, null));
        Assert.IsFalse(NaturalStringComparer.Ordinal.Equals(null, string.Empty));
        Assert.IsFalse(NaturalStringComparer.Ordinal.Equals(string.Empty, null));
    }

    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.GetHashCode(string?)" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenObjIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NaturalStringComparer.Ordinal.GetHashCode(null);
        });

        Assert.AreEqual("obj", ex.ParamName);
    }

    /// <summary>
    /// Verifies that sorting an array containing <see langword="null" /> entries places them before every
    /// non-<see langword="null" /> string.
    /// </summary>
    [TestMethod]
    public void Compare_WhenSortingArrayWithNullEntries_ShouldPlaceNullsFirst()
    {
        var values = new[] { "file2", null, "file1", null };

        Array.Sort(values, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(new[] { null, null, "file1", "file2" }, values);
    }
}
