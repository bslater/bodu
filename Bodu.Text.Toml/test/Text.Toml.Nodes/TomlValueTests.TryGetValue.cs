// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValueTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies <see cref="TomlValue.TryGetValue{T}" />, including a float that does not narrow to <see cref="float" />.
/// </summary>
public partial class TomlValueTests
{
    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> returns <see langword="false" /> for a finite float
    /// outside the <see cref="float" /> range, just past its largest finite magnitude or far past it, positive or
    /// negative, rather than converting it to an infinity.
    /// </summary>
    /// <param name="stored">The finite float outside the range.</param>
    [TestMethod]
    [DataRow(3.4028236e38, DisplayName = "just past float.MaxValue")]
    [DataRow(-3.4028236e38, DisplayName = "just past float.MinValue")]
    [DataRow(1e300, DisplayName = "far past float.MaxValue")]
    [DataRow(-1e300, DisplayName = "far past float.MinValue")]
    public void TryGetValue_WhenFloatIsOutsideSingleRange_ForSingle_ShouldReturnFalse(double stored)
    {
        TomlValue value = TomlValue.Create(stored);

        Assert.IsFalse(value.TryGetValue(out float _));
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> converts a float that rounds to the largest finite
    /// <see cref="float" /> magnitude to that value.
    /// </summary>
    /// <param name="stored">The float at the edge of the range.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow(3.4028235e38, float.MaxValue, DisplayName = "rounds to float.MaxValue")]
    [DataRow(-3.4028235e38, float.MinValue, DisplayName = "rounds to float.MinValue")]
    public void TryGetValue_WhenFloatRoundsToLargestFiniteValue_ForSingle_ShouldReturnThatValue(double stored, float expected)
    {
        TomlValue value = TomlValue.Create(stored);

        bool converted = value.TryGetValue(out float single);

        Assert.AreEqual((true, expected), (converted, single));
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> converts an infinity or NaN to the matching
    /// <see cref="float" /> value.
    /// </summary>
    /// <param name="stored">The special float.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow(double.PositiveInfinity, float.PositiveInfinity, DisplayName = "inf")]
    [DataRow(double.NegativeInfinity, float.NegativeInfinity, DisplayName = "-inf")]
    [DataRow(double.NaN, float.NaN, DisplayName = "nan")]
    public void TryGetValue_WhenFloatIsInfinityOrNaN_ForSingle_ShouldReturnMatchingValue(double stored, float expected)
    {
        TomlValue value = TomlValue.Create(stored);

        bool converted = value.TryGetValue(out float single);

        Assert.AreEqual((true, expected), (converted, single));
    }
}
