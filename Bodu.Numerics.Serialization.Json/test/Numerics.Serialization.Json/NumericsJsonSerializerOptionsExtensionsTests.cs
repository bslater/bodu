// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsJsonSerializerOptionsExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" /> registers the expected
/// converters, validates its arguments, and selects the requested <see cref="NumericsJsonPolicy" />.
/// </summary>
[TestClass]
public class NumericsJsonSerializerOptionsExtensionsTests
{
    /// <summary>
    /// Verifies that the extension registers one converter for each shipped numeric type.
    /// </summary>
    [TestMethod]
    public void AddNumericsJsonConverters_WhenCalled_ShouldRegisterExpectedConverters()
    {
        JsonSerializerOptions options = new();

        options.AddNumericsJsonConverters();

        Assert.HasCount(6, options.Converters);
        Assert.Contains(c => c is FractionJsonConverterFactory, options.Converters);
        Assert.Contains(c => c is IntervalJsonConverterFactory, options.Converters);
        Assert.Contains(c => c is DiscreteIntervalJsonConverterFactory, options.Converters);
        Assert.Contains(c => c is IntervalSetJsonConverterFactory, options.Converters);
        Assert.Contains(c => c is BigDecimalJsonConverter, options.Converters);
        Assert.Contains(c => c is ComplexJsonConverterFactory, options.Converters);
    }

    /// <summary>
    /// Verifies that the extension returns its argument so that calls compose fluently.
    /// </summary>
    [TestMethod]
    public void AddNumericsJsonConverters_WhenCalled_ShouldReturnSameInstance()
    {
        JsonSerializerOptions options = new();

        JsonSerializerOptions returned = options.AddNumericsJsonConverters();

        Assert.AreSame(options, returned);
    }

    /// <summary>
    /// Verifies that a null <see cref="JsonSerializerOptions" /> throws <see cref="ArgumentNullException" /> with the
    /// expected <see cref="ArgumentException.ParamName" />.
    /// </summary>
    [TestMethod]
    public void AddNumericsJsonConverters_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        JsonSerializerOptions? options = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = options!.AddNumericsJsonConverters();
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined policy throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void AddNumericsJsonConverters_WhenPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        JsonSerializerOptions options = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = options.AddNumericsJsonConverters((NumericsJsonPolicy)999);
        });
    }
}
