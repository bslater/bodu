// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies that <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> honours each <see cref="FinancialJsonPolicy" /> on
/// both the read and write paths.
/// </summary>
[TestClass]
public partial class MoneyOfTCurrencyJsonConverterPolicyTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the financial converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);
}
