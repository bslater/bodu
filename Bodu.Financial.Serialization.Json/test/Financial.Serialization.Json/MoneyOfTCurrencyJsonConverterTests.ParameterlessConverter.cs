// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterTests.ParameterlessConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyOfTCurrencyJsonConverterTests
{
    /// <summary>
    /// Verifies that the parameterless converter constructor round-trips a typed value through the Strict object shape.
    /// </summary>
    [TestMethod]
    public void ParameterlessConverter_WhenUsed_ShouldRoundTripStrict()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MoneyOfTCurrencyJsonConverter<USD>());

        string json = JsonSerializer.Serialize(new Money<USD>(19.99m), options);

        Assert.AreEqual(new Money<USD>(19.99m), JsonSerializer.Deserialize<Money<USD>>(json, options));
    }
}
