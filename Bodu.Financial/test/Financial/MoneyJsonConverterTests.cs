// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyJsonConverter" /> across the Strict, Lenient, and Compact policies, including the
/// malformed-payload rejection paths.
/// </summary>
[TestClass]
public partial class MoneyJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);
}
