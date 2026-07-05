// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Kat;

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Equals(string?, string?)" /> agrees with
    /// <see cref="NaturalStringComparer.Compare(string?, string?)" /> — a pair is equal exactly when the comparison
    /// sign is zero — across the full ordinal ordering table.
    /// </summary>
    /// <param name="kat">The KAT row supplying the operand pair and the expected comparison sign.</param>
    [TestMethod]
    [DynamicData(
        nameof(CompareOrdinalCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Equals_WhenOrdinal_ShouldAgreeWithCompare(ValidKat<(string Left, string Right), int> kat) =>
        Assert.AreEqual(kat.Expected == 0, NaturalStringComparer.Ordinal.Equals(kat.Input.Left, kat.Input.Right), kat.Name);

    /// <summary>
    /// Verifies that two strings whose digit runs carry the same numeric value but different leading-zero counts —
    /// <c>"file07"</c> and <c>"file7"</c> — are not equal, pinning the leading-zero total-order tiebreak.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDigitRunsDifferOnlyInLeadingZeros_ShouldReturnFalse()
    {
        Assert.IsFalse(NaturalStringComparer.Ordinal.Equals("file07", "file7"));
        Assert.AreNotEqual(0, NaturalStringComparer.Ordinal.Compare("file07", "file7"));
    }

    /// <summary>
    /// Verifies that identical strings are equal and produce identical hash codes under
    /// <see cref="NaturalStringComparer.Ordinal" />.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenStringsAreIdentical_ShouldMatch()
    {
        var comparer = NaturalStringComparer.Ordinal;

        Assert.IsTrue(comparer.Equals("file10", "file10"));
        Assert.AreEqual(comparer.GetHashCode("file10"), comparer.GetHashCode("file10"));
    }

    /// <summary>
    /// Verifies that strings differing only by case are equal under
    /// <see cref="NaturalStringComparer.OrdinalIgnoreCase" /> and hash to the same value.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenIgnoreCasePairIsEqual_ShouldMatch()
    {
        var comparer = NaturalStringComparer.OrdinalIgnoreCase;

        Assert.IsTrue(comparer.Equals("FILE10", "file10"));
        Assert.AreEqual(comparer.GetHashCode("FILE10"), comparer.GetHashCode("file10"));
    }

    /// <summary>
    /// Verifies that a culture-bound comparer produced by
    /// <see cref="NaturalStringComparer.Create(CultureInfo, bool)" /> assigns equal hash codes to strings it
    /// considers equal, including case-folded pairs when case is ignored.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenCultureAwarePairIsEqual_ShouldMatch()
    {
        var comparer = NaturalStringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

        Assert.IsTrue(comparer.Equals("wert2", "WERT2"));
        Assert.AreEqual(comparer.GetHashCode("wert2"), comparer.GetHashCode("WERT2"));
    }

    /// <summary>
    /// Verifies that strings that are unequal under <see cref="NaturalStringComparer.Ordinal" /> — differing text,
    /// differing numeric value, or case differences — report <see langword="false" /> from
    /// <see cref="NaturalStringComparer.Equals(string?, string?)" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStringsDiffer_ShouldReturnFalse()
    {
        var comparer = NaturalStringComparer.Ordinal;

        Assert.IsFalse(comparer.Equals("file2", "file10"));
        Assert.IsFalse(comparer.Equals("filea", "fileb"));
        Assert.IsFalse(comparer.Equals("FILE1", "file1"));
    }
}
