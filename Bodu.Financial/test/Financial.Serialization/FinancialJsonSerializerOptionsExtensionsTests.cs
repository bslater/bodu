// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonSerializerOptionsExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

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

        Assert.AreEqual(3, options.Converters.Count);
        Assert.IsTrue(options.Converters.Any(c => c is MoneyJsonConverterFactory));
        Assert.IsTrue(options.Converters.Any(c => c is MoneyValueJsonConverter));
        Assert.IsTrue(options.Converters.Any(c => c is MoneyBagJsonConverter));
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
