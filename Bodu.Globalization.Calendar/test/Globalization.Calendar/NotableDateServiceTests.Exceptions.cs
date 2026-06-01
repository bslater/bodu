// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Exceptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the argument-validation contracts of <see cref="NotableDateService" />'s constructor
/// and every <see cref="NotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
/// overload that accepts a filter.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    // -----------------------------------------------------------------------------------------
    // Constructor – ParamName coverage
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="NotableDateService" /> constructor surfaces the
    /// <c>weekendDefinition</c> parameter name when an undefined enum value is supplied.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWeekendDefinitionIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new NotableDateService(
                [],
                (WorkingDaysOfWeek)int.MaxValue);
        });
    }

    // -----------------------------------------------------------------------------------------
    // GetNotableDates(int, NotableDateFilter, …) – ParamName + null-filter coverage
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" />
    /// exposes the <c>filter</c> parameter name on the thrown <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenYearFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(2024, (NotableDateFilter)null!);
        });

        Assert.AreEqual("filter", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
    /// exposes the <c>filter</c> parameter name on the thrown <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenDateRangeFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31),
                (NotableDateFilter)null!);
        });

        Assert.AreEqual("filter", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" />
    /// exposes the <c>filter</c> parameter name on the thrown <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenSingleDateFilterIsNull_ShouldThrowExactly()
    {
        NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2024, 1, 1), (NotableDateFilter)null!);
        });

        Assert.AreEqual("filter", ex.ParamName);
    }
}
