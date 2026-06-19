// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterPolicyTests.DefaultAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRatePairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the attribute-driven default serializer emits the canonical object form.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var pair = new ExchangeRatePair("USD", "JPY");

        string json = JsonSerializer.Serialize(pair);

        Assert.AreEqual("{\"from\":\"USD\",\"to\":\"JPY\"}", json);
    }
}
