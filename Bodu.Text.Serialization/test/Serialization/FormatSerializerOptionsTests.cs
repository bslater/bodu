// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Infrastructure;

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies the configuration, freezing, and converter-resolution behavior of <see cref="FormatSerializerOptions" />.
/// </summary>
[TestClass]
public class FormatSerializerOptionsTests
{
    /// <summary>
    /// Verifies that setting a property after the options have been used throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenOptionsAreReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new FormatSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.PropertyNameCaseInsensitive = false;
        });
    }

    /// <summary>
    /// Verifies that a negative maximum depth is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new FormatSerializerOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            options.MaxDepth = -1;
        });
    }

    /// <summary>
    /// Verifies that setting the maximum depth to zero restores the default depth.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToZero_ShouldUseDefault()
    {
        var options = new FormatSerializerOptions { MaxDepth = 0 };

        Assert.AreEqual(FormatSerializerOptions.DefaultMaxDepth, options.MaxDepth);
    }

    /// <summary>
    /// Verifies that resolving the same type twice returns the cached converter instance.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenCalledTwice_ShouldReturnCachedInstance()
    {
        var options = new FormatSerializerOptions();

        FormatConverter first = options.GetConverter(typeof(int));
        FormatConverter second = options.GetConverter(typeof(int));

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that a registered converter takes precedence over the built-in converter for the same type.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenUserConverterRegistered_ShouldTakePrecedence()
    {
        var converter = new ReverseStringConverter();
        var options = new FormatSerializerOptions();
        options.Converters.Add(converter);

        FormatConverter resolved = options.GetConverter(typeof(string));

        Assert.AreSame(converter, resolved);
    }
}
