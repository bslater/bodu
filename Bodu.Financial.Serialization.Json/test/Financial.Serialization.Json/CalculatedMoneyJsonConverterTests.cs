// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies <see cref="CalculatedMoneyJsonConverter" /> across the Strict, Lenient, and Compact policies, focusing on
/// the deferred-arithmetic tier's defining property: the full, unrounded <see cref="decimal" /> precision - including
/// trailing zeros - survives a round-trip.
/// </summary>
[TestClass]
public partial class CalculatedMoneyJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);
}
