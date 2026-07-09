// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterPolicyTests.DefaultAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization;

public partial class CurrencyPairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the attribute-driven default serializer emits the canonical object form.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var pair = new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY);

        string json = JsonSerializer.Serialize(pair);

        Assert.AreEqual("{\"from\":\"USD\",\"to\":\"JPY\"}", json);
    }
}
