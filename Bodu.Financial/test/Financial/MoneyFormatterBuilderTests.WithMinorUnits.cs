// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterBuilderTests.WithMinorUnits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyFormatterBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="MoneyFormatterBuilder.WithMinorUnits(int)" /> rejects a precision outside 0 to 28 with
    /// <see cref="ArgumentOutOfRangeException" /> naming the argument, rather than deferring the failure to the built
    /// formatter's <c>Format</c> methods.
    /// </summary>
    /// <param name="minorUnits">The out-of-range precision.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    [DataRow(int.MinValue)]
    public void WithMinorUnits_WhenOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        var builder = new MoneyFormatterBuilder();

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.WithMinorUnits(minorUnits);
        });

        Assert.AreEqual("minorUnits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyFormatterBuilder.WithMinorUnits(int)" /> accepts the bounds 0 and 28 and the built
    /// formatter renders that many fractional digits.
    /// </summary>
    /// <param name="minorUnits">The precision at a bound of the supported range.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(28)]
    public void WithMinorUnits_WhenAtRangeBound_ShouldRenderThatPrecision(int minorUnits)
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithNumericOnly()
            .WithGrouping(false)
            .WithCulture(CultureInfo.InvariantCulture)
            .WithMinorUnits(minorUnits)
            .Build();

        string text = formatter.Format(new Money(1.5m, CurrencyCode.USD));

        Assert.AreEqual(1.5m.ToString("F" + minorUnits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture), text);
    }
}
