// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Compare.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Gets the ordering table for <see cref="NaturalStringComparer.Ordinal" />. Each row carries a left/right pair
    /// and the expected sign of <c>Compare(Left, Right)</c>.
    /// </summary>
    public static IEnumerable<object[]> CompareOrdinalCases
    {
        get
        {
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Identical strings", ("file1", "file1"), 0) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Both empty", (string.Empty, string.Empty), 0) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Empty before non-empty", (string.Empty, "a"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Numeric suffix 2 before 10", ("file2", "file10"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Numeric suffix 10 after 2", ("file10", "file2"), 1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Prefix before longer string", ("file", "file1"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Plain text ordinal order", ("apple", "banana"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Leading zeros tiebreak 07 after 7", ("07", "7"), 1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Leading zeros tiebreak 7 before 07", ("7", "07"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("All-zero runs 0 before 00", ("0", "00"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Zero-padded equal value a02 after a2", ("a02", "a2"), 1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Later text overrides zero tiebreak", ("file07", "file7z"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Multi-run second run decides", ("a2b10", "a2b9"), 1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Multi-run first run decides", ("a2b10", "a10b2"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("30-digit run after 29-digit run", (new string('9', 30), new string('9', 29)), 1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Equal-length long runs differ in last digit", ("123456789012345678901234567890", "123456789012345678901234567891"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Long zero-padded run equals magnitude of trimmed run", ("0000000000000000000000000000001", "1x"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Digit sorts before non-digit text", ("file1", "file!"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Digit sorts before letter", ("1", "a"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Hyphen is a plain character not a sign", ("a-2", "a-10"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Uppercase before lowercase under ordinal", ("File2", "file2"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Case differs so ordinal not equal", ("FILE10", "file10"), -1) };
        }
    }

    /// <summary>
    /// Gets the ordering table for <see cref="NaturalStringComparer.OrdinalIgnoreCase" />. Each row carries a
    /// left/right pair and the expected sign of <c>Compare(Left, Right)</c>.
    /// </summary>
    public static IEnumerable<object[]> CompareOrdinalIgnoreCaseCases
    {
        get
        {
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Case-insensitive equal", ("FILE10", "file10"), 0) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Mixed case numeric order", ("FILE2", "file10"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Mixed case text order", ("Apple", "BANANA"), -1) };
            yield return new object[] { new ValidKat<(string Left, string Right), int>("Leading zeros still unequal when case folded", ("FILE07", "file7"), 1) };
        }
    }

    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Compare(string?, string?)" /> on
    /// <see cref="NaturalStringComparer.Ordinal" /> returns the expected sign for each ordering row.
    /// </summary>
    /// <param name="kat">The KAT row supplying the operand pair and the expected comparison sign.</param>
    [TestMethod]
    [DynamicData(
        nameof(CompareOrdinalCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compare_WhenOrdinal_ShouldReturnExpectedSign(ValidKat<(string Left, string Right), int> kat) =>
        Assert.AreEqual(kat.Expected, Math.Sign(NaturalStringComparer.Ordinal.Compare(kat.Input.Left, kat.Input.Right)), kat.Name);

    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Compare(string?, string?)" /> on
    /// <see cref="NaturalStringComparer.Ordinal" /> is antisymmetric: swapping the operands negates the sign of every
    /// ordering row.
    /// </summary>
    /// <param name="kat">The KAT row supplying the operand pair and the expected comparison sign.</param>
    [TestMethod]
    [DynamicData(
        nameof(CompareOrdinalCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compare_WhenOrdinalOperandsSwapped_ShouldReturnNegatedSign(ValidKat<(string Left, string Right), int> kat) =>
        Assert.AreEqual(-kat.Expected, Math.Sign(NaturalStringComparer.Ordinal.Compare(kat.Input.Right, kat.Input.Left)), kat.Name);

    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Compare(string?, string?)" /> on
    /// <see cref="NaturalStringComparer.OrdinalIgnoreCase" /> returns the expected sign for each case-folding row.
    /// </summary>
    /// <param name="kat">The KAT row supplying the operand pair and the expected comparison sign.</param>
    [TestMethod]
    [DynamicData(
        nameof(CompareOrdinalIgnoreCaseCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compare_WhenOrdinalIgnoreCase_ShouldReturnExpectedSign(ValidKat<(string Left, string Right), int> kat) =>
        Assert.AreEqual(kat.Expected, Math.Sign(NaturalStringComparer.OrdinalIgnoreCase.Compare(kat.Input.Left, kat.Input.Right)), kat.Name);
}
