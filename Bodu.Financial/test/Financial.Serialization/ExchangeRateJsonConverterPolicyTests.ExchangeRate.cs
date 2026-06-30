// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.ExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRateJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the <c>[JsonConverter]</c> attribute remains present.
    /// </summary>
    [TestMethod]
    public void ExchangeRate_WhenInspected_ShouldDeclareJsonConverterAttribute() => Assert.IsTrue(typeof(ExchangeRate).IsDefined(typeof(JsonConverterAttribute), inherit: false));
}
