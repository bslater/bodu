// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse" /> reports failure and yields the default for invalid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInvalid_ShouldReturnFalseAndDefault()
    {
        bool parsed = TerritoryCode.TryParse("nope!", out TerritoryCode result);

        Assert.AreEqual((false, true), (parsed, result.IsEmpty));
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse" /> reports failure for <see langword="null" /> and empty input.
    /// </summary>
    /// <param name="value">The null or empty input under test.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void TryParse_WhenNullOrEmpty_ShouldReturnFalse(string? value)
    {
        Assert.IsFalse(TerritoryCode.TryParse(value, out _));
    }

    /// <summary>
    /// Verifies that a subdivision containing a non-alphanumeric character is rejected by <c>TryParse</c>.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenSubdivisionHasInvalidCharacter_ShouldReturnFalse() =>
        Assert.IsFalse(TerritoryCode.TryParse("US-1$", out _));
}
