// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.Always.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentTriggerMatrixTests
{
    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.Always" /> trigger fires regardless of the anchor weekday or year.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2022)] // Saturday
    [DataRow(2023)] // Sunday
    [DataRow(2024)] // Monday
    [DataRow(2026)] // Thursday
    public void Always_WhenAnyAnchor_ShouldAlwaysFire(int year) =>
        AssertActivation(TriggerService, "always-h", year, "always");
}
