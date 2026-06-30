// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.ExchangeRatePair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialJsonConverterGuardsTests
{

    /// <summary>
    /// Verifies that a compact pair whose slash-separated codes are not valid ISO codes is rejected with a
    /// <see cref="JsonException" /> wrapping the constructor's argument error.
    /// </summary>
    [TestMethod]
    public void ExchangeRatePair_WhenCompactCodesInvalid_ShouldThrowJsonException()
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("\"XX/YY\"", options));
    }
}
