// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Sort.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that <see cref="Array.Sort{T}(T[], IComparer{T}?)" /> with
    /// <see cref="NaturalStringComparer.Ordinal" /> orders the classic natsort sample
    /// <c>["z11", "z2", "z1", "z10"]</c> numerically rather than lexicographically.
    /// </summary>
    [TestMethod]
    public void Compare_WhenArraySortOverNatsortClassic_ShouldOrderNumerically()
    {
        var values = new[] { "z11", "z2", "z1", "z10" };

        Array.Sort(values, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(new[] { "z1", "z2", "z10", "z11" }, values);
    }

    /// <summary>
    /// Verifies that <see cref="Enumerable.OrderBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, IComparer{TKey}?)" />
    /// with <see cref="NaturalStringComparer.OrdinalIgnoreCase" /> orders mixed-case names by numeric value while
    /// folding case.
    /// </summary>
    [TestMethod]
    public void Compare_WhenOrderByWithOrdinalIgnoreCase_ShouldOrderNumericallyIgnoringCase()
    {
        var values = new[] { "File10", "file2", "FILE1" };

        var ordered = values.OrderBy(value => value, NaturalStringComparer.OrdinalIgnoreCase).ToArray();

        CollectionAssert.AreEqual(new[] { "FILE1", "file2", "File10" }, ordered);
    }

    /// <summary>
    /// Verifies that sorting multi-run names with <see cref="NaturalStringComparer.Ordinal" /> compares every
    /// embedded digit run numerically, not just the first.
    /// </summary>
    [TestMethod]
    public void Compare_WhenArraySortOverMultiRunNames_ShouldCompareEveryRunNumerically()
    {
        var values = new[] { "a2b10", "a10b2", "a2b9", "a2b2" };

        Array.Sort(values, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(new[] { "a2b2", "a2b9", "a2b10", "a10b2" }, values);
    }

    /// <summary>
    /// Verifies that sorting zero-padded counters with <see cref="NaturalStringComparer.Ordinal" /> groups equal
    /// numeric values together with the less-padded form first.
    /// </summary>
    [TestMethod]
    public void Compare_WhenArraySortOverZeroPaddedCounters_ShouldPlaceLessPaddedFormFirst()
    {
        var values = new[] { "item007", "item7", "item10", "item07" };

        Array.Sort(values, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(new[] { "item7", "item07", "item007", "item10" }, values);
    }
}
