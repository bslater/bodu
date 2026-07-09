// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{
    /// <summary>
    /// Verifies that the constructor rejects the currency-less <see cref="CurrencyCode.None" /> with
    /// <see cref="ArgumentOutOfRangeException" /> for the parameter <c>code</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCodeIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new CalculatedMoney(1m, CurrencyCode.None);
            },
            "code");
    }

    /// <summary>
    /// Verifies that the constructor rejects an undefined <see cref="CurrencyCode" /> with
    /// <see cref="ArgumentOutOfRangeException" /> for the parameter <c>code</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCodeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new CalculatedMoney(1m, (CurrencyCode)9999);
            },
            "code");
    }
}
