// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSequenceExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="NotableDateSequenceExtensions" /> sequence transforms over resolved occurrences.
/// </summary>
[TestClass]
public sealed partial class NotableDateSequenceExtensionsTests
{
    /// <summary>
    /// Builds a service over the adjacent-holidays fixture (Christmas and Boxing Day, each weekend-substituted with
    /// <c>skipNonWorkingDates</c> under an observed-only emission).
    /// </summary>
    /// <returns>A service for the adjacent-holidays fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("adjacent-holidays.xml");

    /// <summary>
    /// Creates a hand-built occurrence with representative defaults, so edge-case tests can state only the fields they
    /// exercise.
    /// </summary>
    /// <param name="date">The emitted date.</param>
    /// <param name="actualDate">The calculated date, passed through verbatim.</param>
    /// <param name="isObserved">The observed flag.</param>
    /// <param name="notableDateId">The notable-date concept id.</param>
    /// <param name="ruleId">The rule id.</param>
    /// <param name="durationDays">The number of days the occurrence spans.</param>
    /// <param name="adjustmentPolicyId">The adjustment-policy id.</param>
    /// <param name="adjustmentReason">The adjustment reason.</param>
    /// <returns>The hand-built occurrence.</returns>
    private static NotableDate Create(
        DateOnly date,
        DateOnly? actualDate = null,
        bool isObserved = false,
        string notableDateId = "sample-day",
        string ruleId = "rule",
        int durationDays = 1,
        string? adjustmentPolicyId = null,
        string? adjustmentReason = null) =>
        new(
            date,
            actualDate,
            isObserved,
            new NotableDateRuleIdentity("test", notableDateId, ruleId),
            "Sample Day",
            "AU",
            NotableDateCategory.PublicHoliday,
            100,
            durationDays,
            true,
            [],
            adjustmentPolicyId,
            adjustmentReason);
}
