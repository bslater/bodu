// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.MoneyTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class MoneyOfTCurrencyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the shipped <c>[JsonConverter]</c> attribute remains present so default (no-options)
    /// serialization picks the strict shape with no consumer configuration.
    /// </summary>
    [TestMethod]
    public void MoneyTypes_WhenInspected_ShouldStillDeclareJsonConverterAttribute()
    {
        Assert.IsTrue(typeof(Money<USD>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(Money).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(MoneyBag).IsDefined(typeof(JsonConverterAttribute), inherit: false));
    }
}
