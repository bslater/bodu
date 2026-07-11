// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" />: the parameterless (Strict) constructor, the
/// type-parameter currency-consistency guard, and the malformed-payload rejection paths.
/// </summary>
[TestClass]
public partial class MoneyOfTCurrencyJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);
}
