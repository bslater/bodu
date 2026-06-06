// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAssert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides field-by-field assertions over a <see cref="NotableDate" /> so that test failures pinpoint the differing
/// field.
/// </summary>
internal static class NotableDateAssert
{
    /// <summary>
    /// Asserts that a resolved occurrence matches the expected field values.
    /// </summary>
    /// <param name="actual">The resolved occurrence under test.</param>
    /// <param name="date">The expected emitted date.</param>
    /// <param name="actualDate">The expected calculated date.</param>
    /// <param name="isObserved">The expected observed flag.</param>
    /// <param name="notableDateId">The expected notable-date id.</param>
    /// <param name="ruleId">The expected rule id.</param>
    /// <param name="displayName">The expected display name.</param>
    /// <param name="territory">The expected territory code.</param>
    /// <param name="category">The expected category.</param>
    /// <param name="adjustmentPolicyId">The expected adjustment-policy id, or <see langword="null" />.</param>
    public static void AssertEqual(
        NotableDate actual,
        DateOnly date,
        DateOnly? actualDate,
        bool isObserved,
        string notableDateId,
        string ruleId,
        string displayName,
        string territory,
        NotableDateCategory category,
        string? adjustmentPolicyId)
    {
        Assert.AreEqual(date, actual.Date, "Date");
        Assert.AreEqual(actualDate, actual.ActualDate, "ActualDate");
        Assert.AreEqual(isObserved, actual.IsObserved, "IsObserved");
        Assert.AreEqual(notableDateId, actual.NotableDateId, "NotableDateId");
        Assert.AreEqual(ruleId, actual.RuleId, "RuleId");
        Assert.AreEqual(displayName, actual.DisplayName, "DisplayName");
        Assert.AreEqual(territory, actual.TerritoryCode, "TerritoryCode");
        Assert.AreEqual(category, actual.Category, "Category");
        Assert.AreEqual(adjustmentPolicyId, actual.AdjustmentPolicyId, "AdjustmentPolicyId");
    }
}
