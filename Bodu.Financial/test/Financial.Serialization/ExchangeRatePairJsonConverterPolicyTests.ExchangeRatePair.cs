// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterPolicyTests.ExchangeRatePair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRatePairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the <c>[JsonConverter]</c> attribute remains present so the no-options happy path picks the
    /// strict shape automatically.
    /// </summary>
    [TestMethod]
    public void ExchangeRatePair_WhenInspected_ShouldDeclareJsonConverterAttribute() => Assert.IsTrue(typeof(ExchangeRatePair).IsDefined(typeof(JsonConverterAttribute), inherit: false));
}
