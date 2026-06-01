// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Extensions.Contracts;

/// <summary>
/// Drives <see cref="NotableDateTemporalExtensionContractTests{TDate}" /> against the
/// <see cref="DateTime" /> extension methods exposed via <see cref="NotableDateTimeExtensions" />.
/// Territory-specific holiday tables are exercised by the existing
/// <c>NotableDateTimeExtensionsTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class NotableDateTimeExtensionsContractTests
    : NotableDateTemporalExtensionContractTests<DateTime>
{
    /// <inheritdoc />
    protected override DateTime CreateDate(int year, int month, int day) =>
        new(year, month, day);

    /// <inheritdoc />
    protected override DateTime AddWorkingDays(DateTime date, int days) =>
        date.AddWorkingDays(days);

    /// <inheritdoc />
    protected override DateTime NextWorkingDay(DateTime date) =>
        date.NextWorkingDay();
}
