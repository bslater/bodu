// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCookbookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the load and validation pipeline of <see cref="NotableDateCookbook" /> against the minimal cookbook.
/// </summary>
[TestClass]
public sealed class NotableDateCookbookTests
{
    /// <summary>
    /// Verifies that loading the minimal cookbook validates successfully and reports three notable-date concepts, five
    /// rules, and one adjustment policy. (T01)
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void LoadMinimalCookbook_ReturnsExpectedCounts()
    {
        NotableDateResource resource = MinimalCookbook.Load();

        Assert.AreEqual(3, resource.NotableDates.Count, "notable-date count");
        Assert.AreEqual(5, resource.RuleCount, "rule count");
        Assert.AreEqual(1, resource.AdjustmentPolicies.Count, "adjustment-policy count");
    }
}
