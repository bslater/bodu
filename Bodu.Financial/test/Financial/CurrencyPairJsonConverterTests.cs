// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="CurrencyPairJsonConverter" /> across the object and compact <c>"FROM/TO"</c> shapes,
/// including the malformed-payload rejection paths.
/// </summary>
[TestClass]
public partial class CurrencyPairJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);
}
