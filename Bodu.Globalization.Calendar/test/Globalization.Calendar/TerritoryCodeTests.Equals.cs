// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.Equals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that two codes differing only in case are equal after normalization, are not unequal, and share a hash
    /// code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameCodeDifferentCase_ShouldBeEqual()
    {
        var a = TerritoryCode.Parse("au-nsw");
        var b = TerritoryCode.Parse("AU-NSW");

        Assert.AreEqual(
            (true, false, true),
            (a == b, a != b, a.GetHashCode() == b.GetHashCode()));
    }

    /// <summary>
    /// Verifies that two codes differing in subdivision are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentSubdivision_ShouldNotBeEqual()
    {
        Assert.AreNotEqual(TerritoryCode.Parse("AU-NSW"), TerritoryCode.Parse("AU-VIC"));
    }

    /// <summary>
    /// Verifies that the boxed <see cref="object.Equals(object)" /> override matches an equal code and rejects a
    /// non-<see cref="TerritoryCode" /> object.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedAsObject_ShouldMatchValue()
    {
        var code = TerritoryCode.Parse("AU-NSW");

        Assert.IsTrue(code.Equals((object)TerritoryCode.Parse("AU-NSW")));
        Assert.IsFalse(code.Equals((object)"AU-NSW"));
    }
}
