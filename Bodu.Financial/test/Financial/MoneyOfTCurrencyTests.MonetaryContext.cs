// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.MonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Multiply(decimal, MonetaryContext)" /> applies the context's rounding
    /// strategy at the currency's minor units.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenContextUsesAwayFromZero_ShouldRoundMidpointAway()
    {
        // 2.45 * 0.5 = 1.225 produces the midpoint inside the multiplication, so the rounding strategy is observable.
        Money<USD> value = new(2.45m);

        Money<USD> banker = value.Multiply(0.5m, MonetaryContext.Default);
        Money<USD> away = value.Multiply(0.5m, MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero });

        Assert.AreEqual(new Money<USD>(1.22m), banker);
        Assert.AreEqual(new Money<USD>(1.23m), away);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Multiply(decimal, MonetaryContext)" /> throws
    /// <see cref="ArgumentNullException" /> when the context is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenContextIsNull_ShouldThrowArgumentNullException()
    {
        Money<USD> value = new(10m);

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = value.Multiply(2m, (MonetaryContext)null!));
    }
}
