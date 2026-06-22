// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateWorkbookParserTests.RecoverDecimal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

/// <content>
/// Verifies that workbook cell values are recovered as the decimal rate RBA published, across the range of magnitudes
/// and decimal precisions the era files contain.
/// </content>
public partial class RbaExchangeRateWorkbookParserTests
{
    /// <summary>
    /// Gets the decimal-recovery known-answer rows: each pairs the <see langword="double" /> stored in a workbook cell
    /// with the decimal RBA published for it. The rows span sub-unit and large magnitudes, two to eight decimal places,
    /// and stored values that sit one unit in the last place from the published figure.
    /// </summary>
    /// <returns>One object array per known-answer row.</returns>
    public static IEnumerable<object[]> RecoverDecimalCases =>
        new[]
        {
            new ValidKat<double, decimal>("four decimal places, sub-unit (USD)", 0.6828, 0.6828m),
            new ValidKat<double, decimal>("four decimal places, single digit (CNY)", 4.6994, 4.6994m),
            new ValidKat<double, decimal>("two decimal places, three digit (JPY)", 112.71, 112.71m),
            new ValidKat<double, decimal>("two decimal places, large magnitude", 841.23, 841.23m),
            new ValidKat<double, decimal>("integer rate, no decimal places", 9019d, 9019m),
            new ValidKat<double, decimal>("eight decimal places preserved", 1.23456789, 1.23456789m),
            new ValidKat<double, decimal>("stored one ULP below published (ZAR 2010-01-04)", 6.6754999999999995, 6.6755m),
            new ValidKat<double, decimal>("stored one ULP above published (SAR 2011-02-10)", 3.7781000000000002, 3.7781m),
        }
        .Select(kat => new object[] { kat });

    /// <summary>
    /// Verifies that <see cref="RbaExchangeRateWorkbookParser.RecoverDecimal(double)" /> returns the decimal RBA
    /// published for a workbook cell regardless of the value's magnitude or decimal precision, including stored doubles
    /// that differ from the published figure by one unit in the last place.
    /// </summary>
    /// <param name="kat">The known-answer row pairing a workbook double with its published decimal.</param>
    [TestMethod]
    [DynamicData(nameof(RecoverDecimalCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RecoverDecimal_WhenWorkbookDouble_ShouldReturnPublishedDecimal(ValidKat<double, decimal> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        decimal actual = RbaExchangeRateWorkbookParser.RecoverDecimal(kat.Input);

        Assert.AreEqual(kat.Expected, actual);
    }
}
