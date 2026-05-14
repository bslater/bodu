// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.Quarter.InvalidProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Quarter(DateOnly, IQuarterDefinitionProvider)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied provider returns a quarter number outside [1, 4].
    /// </summary>
    [TestMethod]
    public void Quarter_WithProvider_WhenProviderReturnsInvalidQuarter_ShouldThrowExactly()
    {
        var provider = new InvalidQuarterProvider();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2024, 6, 15).Quarter(provider);
        });
    }

    /// <summary>
    /// A test-only <see cref="IQuarterDefinitionProvider" /> that intentionally returns a quarter number outside the valid range
    /// to exercise validation in <see cref="DateOnlyExtensions.Quarter(DateOnly, IQuarterDefinitionProvider)" />. Other interface
    /// members throw <see cref="NotImplementedException" /> because the validation runs before they are reached.
    /// </summary>
    private sealed class InvalidQuarterProvider
        : IQuarterDefinitionProvider
    {
        public int GetQuarter(DateTime dateTime) => 99;

        public int GetQuarter(DateOnly dateOnly) => 99;

        public DateTime GetQuarterEnd(DateTime dateTime) => throw new NotImplementedException();

        public DateTime GetQuarterEnd(int quarter) => throw new NotImplementedException();

        public DateTime GetQuarterEnd(int quarter, int fiscalYear) => throw new NotImplementedException();

        public DateOnly GetQuarterEndDate(DateOnly dateOnly) => throw new NotImplementedException();

        public DateOnly GetQuarterEndDate(int quarter) => throw new NotImplementedException();

        public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) => throw new NotImplementedException();

        public DateTime GetQuarterStart(DateTime dateTime) => throw new NotImplementedException();

        public DateTime GetQuarterStart(int quarter) => throw new NotImplementedException();

        public DateTime GetQuarterStart(int quarter, int fiscalYear) => throw new NotImplementedException();

        public DateOnly GetQuarterStartDate(DateOnly dateOnly) => throw new NotImplementedException();

        public DateOnly GetQuarterStartDate(int quarter) => throw new NotImplementedException();

        public DateOnly GetQuarterStartDate(int quarter, int fiscalYear) => throw new NotImplementedException();

        public bool Is53WeekFiscalYear(int fiscalYear) => throw new NotImplementedException();

        public int GetWeeksInFiscalYear(int fiscalYear) => throw new NotImplementedException();
    }
}
