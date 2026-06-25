// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonSerializerOptionsExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" /> registers the
/// expected converters, validates its arguments, and selects the requested <see cref="FinancialJsonPolicy" />.
/// </summary>
[TestClass]
public class FinancialJsonSerializerOptionsExtensionsTests
{
    /// <summary>
    /// Verifies that the extension registers one converter for each shipped monetary type.
    /// </summary>
    [TestMethod]
    public void AddFinancialJsonConverters_WhenCalled_ShouldRegisterExpectedConverters()
    {
        JsonSerializerOptions options = new();

        options.AddFinancialJsonConverters();

        Assert.HasCount(5, options.Converters);
        Assert.Contains(c => c is MoneyOfTCurrencyJsonConverterFactory, options.Converters);
        Assert.Contains(c => c is MoneyJsonConverter, options.Converters);
        Assert.Contains(c => c is MoneyBagJsonConverter, options.Converters);
        Assert.Contains(c => c is ExchangeRateJsonConverter, options.Converters);
        Assert.Contains(c => c is ExchangeRatePairJsonConverter, options.Converters);
    }

    /// <summary>
    /// Verifies that the extension returns its argument so that calls compose fluently.
    /// </summary>
    [TestMethod]
    public void AddFinancialJsonConverters_WhenCalled_ShouldReturnSameInstance()
    {
        JsonSerializerOptions options = new();

        JsonSerializerOptions returned = options.AddFinancialJsonConverters();

        Assert.AreSame(options, returned);
    }

    /// <summary>
    /// Verifies that a null <see cref="JsonSerializerOptions" /> throws <see cref="ArgumentNullException" /> with the
    /// expected <see cref="ArgumentException.ParamName" />.
    /// </summary>
    [TestMethod]
    public void AddFinancialJsonConverters_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        JsonSerializerOptions? options = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = options!.AddFinancialJsonConverters();
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined policy throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void AddFinancialJsonConverters_WhenPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        JsonSerializerOptions options = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = options.AddFinancialJsonConverters((FinancialJsonPolicy)999);
        });
    }
}
