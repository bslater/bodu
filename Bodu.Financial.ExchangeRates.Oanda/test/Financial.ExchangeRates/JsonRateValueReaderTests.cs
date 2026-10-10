// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>New tests for the shared scalar conversion, independent of provider JSON document shapes.</summary>
[TestClass]
public sealed class JsonRateValueReaderTests
{
    [TestMethod]
    [DataRow("123.450", true)]
    [DataRow("\"123.450\"", true)]
    [DataRow("0", false)]
    [DataRow("-1.5", false)]
    [DataRow("null", false)]
    [DataRow("\"invalid\"", false)]
    public void TryReadPositiveDecimal_WhenJsonScalar_ShouldPreserveRateRules(string json, bool expected)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        bool success = JsonRateValueReader.TryReadPositiveDecimal(document.RootElement, out decimal result);
        Assert.AreEqual(expected, success);
        if (success)
            Assert.AreEqual(123.450m, result);
    }

    [TestMethod]
    [DataRow("1700000000000", true)]
    [DataRow("\"1700000000000\"", true)]
    [DataRow("1.2", false)]
    [DataRow("null", false)]
    public void TryReadUnixMilliseconds_WhenJsonScalar_ShouldPreserveTimestampRules(string json, bool expected)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        bool success = JsonRateValueReader.TryReadUnixMilliseconds(document.RootElement, out long result);
        Assert.AreEqual(expected, success);
        if (success)
            Assert.AreEqual(1700000000000L, result);
    }
}
