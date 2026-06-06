// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the load and validation pipeline of <see cref="NotableDateResourceLoader" /> against the minimal notable-date document.
/// </summary>
[TestClass]
public sealed class NotableDateResourceLoaderTests
{
    /// <summary>
    /// Verifies that loading the minimal notable-date document validates successfully and reports three notable-date concepts, five
    /// rules, and one adjustment policy. (T01)
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void LoadMinimalNotableDates_ReturnsExpectedCounts()
    {
        NotableDateResource resource = MinimalNotableDates.Load();

        Assert.HasCount(3, resource.NotableDates, "notable-date count");
        Assert.AreEqual(5, resource.RuleCount, "rule count");
        Assert.HasCount(1, resource.AdjustmentPolicies, "adjustment-policy count");
    }
}
