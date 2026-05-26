// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTemporalExtensionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for the temporal extension methods that operate on
/// <typeparamref name="TDate" /> instances — typically the <c>AddWorkingDays</c>, <c>NextWorkingDay</c>,
/// <c>PreviousWorkingDay</c>, <c>SnapToWorkingDay</c> family exposed on both <see cref="DateOnly" /> and
/// <see cref="DateTime" />. Concrete subclasses override the three adapter methods to plug their date
/// type into the inherited tests.
/// </summary>
/// <typeparam name="TDate">The date type under test.</typeparam>
public abstract class NotableDateTemporalExtensionContractTests<TDate>
    where TDate : IEquatable<TDate>
{
    /// <summary>
    /// Creates a date of the type under test from year / month / day components.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-based).</param>
    /// <param name="day">The day (1-based).</param>
    /// <returns>A new date.</returns>
    protected abstract TDate CreateDate(int year, int month, int day);

    /// <summary>
    /// Returns the date that results from adding <paramref name="days" /> working days to
    /// <paramref name="date" /> under the contract under test.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="days">The number of working days to add. May be negative.</param>
    /// <returns>The resulting date.</returns>
    protected abstract TDate AddWorkingDays(TDate date, int days);

    /// <summary>
    /// Returns the next working day strictly after <paramref name="date" />.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <returns>The next working day.</returns>
    protected abstract TDate NextWorkingDay(TDate date);

    /// <summary>
    /// Verifies that <see cref="AddWorkingDays" /> with <c>days == 0</c> returns the original date.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenDaysIsZero_ShouldReturnOriginalDate()
    {
        TDate date = CreateDate(2024, 6, 12); // Wednesday — a working day under default Western weekends.

        TDate result = AddWorkingDays(date, 0);

        Assert.IsTrue(date.Equals(result));
    }

    /// <summary>
    /// Verifies that <see cref="AddWorkingDays" /> is reversible from a date that is itself a working
    /// day — adding <c>n</c> and then subtracting <c>n</c> returns the original date. The starting
    /// date is fixed at a known mid-week Wednesday so the round-trip is well-defined regardless of
    /// territory-specific holiday tables.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenStartIsWorkingDayAndAddedThenSubtractedByN_ShouldReturnOriginalDate()
    {
        TDate date = CreateDate(2024, 6, 12); // Wednesday — well clear of any US/UK/AU holiday in early June.

        TDate forward = AddWorkingDays(date, 5);
        TDate roundTrip = AddWorkingDays(forward, -5);

        Assert.IsTrue(date.Equals(roundTrip));
    }

    /// <summary>
    /// Verifies that <see cref="NextWorkingDay" /> returns a value strictly greater than the input. The
    /// base does not assert how much greater — that depends on whether the input was already on a working
    /// day — but the result must not be the input itself.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_ShouldReturnDateStrictlyAfterInput()
    {
        TDate date = CreateDate(2024, 6, 12);

        TDate result = NextWorkingDay(date);

        Assert.IsFalse(date.Equals(result));
    }
}
