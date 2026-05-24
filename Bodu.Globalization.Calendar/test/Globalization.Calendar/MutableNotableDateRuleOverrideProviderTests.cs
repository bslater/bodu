// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the runtime-mutation contract, snapshot semantics, and event-raising behaviour of
/// <see cref="MutableNotableDateRuleOverrideProvider" />.
/// </summary>
[TestClass]
public sealed partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Builds a simple fixed-month rule used by tests that do not care about the rule shape itself.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>A new <see cref="NotableDateRule" /> with the supplied identity.</returns>
    private static NotableDateRule Fixed(string name, int month = 1, int day = 1) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
        };
}
