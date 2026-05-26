// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Test.Contracts;

namespace Bodu.Globalization.Calendar.Extensions.Contracts;

/// <summary>
/// Drives <see cref="NotableDateTemporalExtensionContractTests{TDate}" /> against the
/// <see cref="DateOnly" /> extension methods exposed via <see cref="NotableDateOnlyExtensions" />.
/// The contract base verifies the universal zero-day invariant and the add/subtract reversibility
/// invariant; territory-specific holiday tables are exercised by the existing
/// <c>NotableDateOnlyExtensionsTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class NotableDateOnlyExtensionsContractTests
    : NotableDateTemporalExtensionContractTests<DateOnly>
{
    /// <inheritdoc />
    protected override DateOnly CreateDate(int year, int month, int day) =>
        new(year, month, day);

    /// <inheritdoc />
    protected override DateOnly AddWorkingDays(DateOnly date, int days) =>
        date.AddWorkingDays(days);

    /// <inheritdoc />
    protected override DateOnly NextWorkingDay(DateOnly date) =>
        date.NextWorkingDay();
}
