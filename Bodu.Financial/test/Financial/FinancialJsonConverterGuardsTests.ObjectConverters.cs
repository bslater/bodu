// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.ObjectConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialJsonConverterGuardsTests
{

    /// <summary>
    /// Verifies that every object-form converter throws when a property name is required but a non-property token (a
    /// comment) is surfaced instead.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenTokenIsNotaPropertyName_ShouldThrowJsonException()
    {
        AssertReadThrowsJsonException(new MoneyJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"amount\":1}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Strict), "{ /* c */ \"amount\":1}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"balances\":{}}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":{ /* c */ \"USD\":1}}", allowComments: true);
        AssertReadThrowsJsonException(new ExchangeRateJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"from\":\"USD\"}", allowComments: true);
        AssertReadThrowsJsonException(new ExchangeRatePairJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"from\":\"USD\"}", allowComments: true);
    }

    /// <summary>
    /// Verifies that every object-form converter throws when the buffer ends immediately after a property name, before
    /// its value can be read.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenBufferEndsAfterPropertyName_ShouldThrowJsonException()
    {
        AssertReadThrowsJsonException(new MoneyJsonConverter(FinancialJsonPolicy.Strict), "{\"amount\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Strict), "{\"amount\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":{\"USD\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new ExchangeRateJsonConverter(FinancialJsonPolicy.Strict), "{\"from\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new ExchangeRatePairJsonConverter(FinancialJsonPolicy.Strict), "{\"from\":", isFinalBlock: false);
    }

    /// <summary>
    /// Verifies that the object-form converters ignore unknown properties, exercising the skip branch.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenUnknownPropertyPresent_ShouldIgnoreIt()
    {
        Assert.AreEqual(new Money(1m, CurrencyCode.USD), JsonSerializer.Deserialize<Money>("{\"amount\":1,\"currency\":\"USD\",\"note\":\"x\"}"));
        Assert.AreEqual(new Money<USD>(1m), JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1,\"currency\":\"USD\",\"note\":\"x\"}"));

        MoneyBag bag = JsonSerializer.Deserialize<MoneyBag>("{\"note\":\"x\",\"balances\":{\"USD\":1}}")!;
        Assert.AreEqual(new Money(1m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));

        ExchangeRatePair pair = JsonSerializer.Deserialize<ExchangeRatePair>("{\"from\":\"USD\",\"to\":\"JPY\",\"note\":\"x\"}");
        Assert.AreEqual(CurrencyCode.USD, pair.From);

        ExchangeRate rate = JsonSerializer.Deserialize<ExchangeRate>(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-01-15\",\"rate\":1.5,\"provider\":\"x\",\"note\":\"y\"}");
        Assert.AreEqual(CurrencyCode.USD, rate.From);
    }
}
