// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterPolicyTests.CurrencyPair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization;

public partial class CurrencyPairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the <c>[JsonConverter]</c> attribute remains present so the no-options happy path picks the
    /// strict shape automatically.
    /// </summary>
    [TestMethod]
    public void CurrencyPair_WhenInspected_ShouldDeclareJsonConverterAttribute() => Assert.IsTrue(typeof(CurrencyPair).IsDefined(typeof(JsonConverterAttribute), inherit: false));
}
