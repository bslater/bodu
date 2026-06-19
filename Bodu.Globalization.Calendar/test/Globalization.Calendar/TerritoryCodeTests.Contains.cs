// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that a country contains itself and its subdivisions, but a subdivision contains neither its parent nor a
    /// sibling, unrelated countries never contain each other, and the empty code neither contains nor is contained by any
    /// code. A <see langword="null" /> argument denotes the empty (default) <see cref="TerritoryCode" />.
    /// </summary>
    /// <param name="container">The containing code, or <see langword="null" /> for the default code.</param>
    /// <param name="contained">The candidate contained code, or <see langword="null" /> for the default code.</param>
    /// <param name="expected">The expected containment result.</param>
    [TestMethod]
    [DataRow("AU", "AU", true)]           // a country contains itself
    [DataRow("AU", "AU-NSW", true)]       // a country contains its subdivision
    [DataRow("AU-NSW", "AU-NSW", true)]   // a subdivision contains itself
    [DataRow("AU-NSW", "AU", false)]      // a subdivision does not contain its parent
    [DataRow("AU-NSW", "AU-VIC", false)]  // siblings do not contain each other
    [DataRow("AU", "US", false)]          // unrelated countries
    [DataRow(null, "AU", false)]          // the default code contains nothing
    [DataRow("AU", null, false)]          // nothing contains the default code
    public void Contains_WhenCheckedAgainstParentChild_ShouldFollowSemantics(string? container, string? contained, bool expected)
    {
        TerritoryCode containerCode = container is null ? default : TerritoryCode.Parse(container);
        TerritoryCode containedCode = contained is null ? default : TerritoryCode.Parse(contained);

        Assert.AreEqual(expected, containerCode.Contains(containedCode));
    }
}
