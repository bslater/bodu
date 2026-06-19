// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensionsTests.ConvertToTyped.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyExchangeRateExtensionsTests
{

    /// <summary>
    /// Verifies the typed-target overload converts to a strongly-typed <see cref="Money{TTarget}" />.
    /// </summary>
    [TestMethod]
    public void ConvertToTyped_WhenRateAvailable_ShouldReturnTypedAmount()
    {
        Money source = new(100m, "EUR");

        Money<USD> result = source.ConvertTo<USD>(BuildProvider(), s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(new Money<USD>(110m), result);
    }
}
