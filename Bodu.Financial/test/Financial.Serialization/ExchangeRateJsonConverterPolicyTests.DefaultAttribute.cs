// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.DefaultAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRateJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the attribute-driven default emits the canonical object form, with all six fields in
    /// declaration order.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        string json = JsonSerializer.Serialize(SampleRate());

        Assert.AreEqual(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}",
            json);
    }
}
