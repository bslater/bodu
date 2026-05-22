// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.Parse{T}(string)" /> returns the parsed integer using the
    /// invariant culture.
    /// </summary>
    [TestMethod]
    public void Parse_OfInt_WhenInputIsValid_ShouldReturnParsedValue()
    {
        Assert.AreEqual(42, "42".Parse<int>());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Parse{T}(string)" /> parses a double using the invariant
    /// decimal separator regardless of the current culture.
    /// </summary>
    [TestMethod]
    public void Parse_OfDouble_WhenInputUsesInvariantSeparator_ShouldReturnParsedValue()
    {
        Assert.AreEqual(3.14, "3.14".Parse<double>(), 1e-9);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Parse{T}(string)" /> throws <see cref="FormatException" />
    /// when the input is not in a valid format.
    /// </summary>
    [TestMethod]
    public void Parse_OfInt_WhenInputIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = "not-a-number".Parse<int>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Parse{T}(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.Parse<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParse{T}(string, out T)" /> returns <see langword="true" />
    /// and the parsed value for a valid input.
    /// </summary>
    [TestMethod]
    public void TryParse_OfInt_WhenInputIsValid_ShouldReturnTrueAndParsedValue()
    {
        var ok = "42".TryParse(out int result);

        Assert.IsTrue(ok);
        Assert.AreEqual(42, result);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParse{T}(string, out T)" /> returns <see langword="false" />
    /// for an invalid input.
    /// </summary>
    [TestMethod]
    public void TryParse_OfInt_WhenInputIsInvalid_ShouldReturnFalse()
    {
        var ok = "not-a-number".TryParse(out int result);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, result);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParse{T}(string, out T)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.TryParse<int>(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ParseSpan{T}(string)" /> returns the parsed integer.
    /// </summary>
    [TestMethod]
    public void ParseSpan_OfInt_WhenInputIsValid_ShouldReturnParsedValue()
    {
        Assert.AreEqual(42, "42".ParseSpan<int>());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ParseSpan{T}(string)" /> throws
    /// <see cref="FormatException" /> for an invalid input.
    /// </summary>
    [TestMethod]
    public void ParseSpan_WhenInputIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = "not-a-number".ParseSpan<int>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ParseSpan{T}(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseSpan_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ParseSpan<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParseSpan{T}(string, out T)" /> returns
    /// <see langword="true" /> and the parsed value for a valid input.
    /// </summary>
    [TestMethod]
    public void TryParseSpan_OfInt_WhenInputIsValid_ShouldReturnTrueAndParsedValue()
    {
        var ok = "42".TryParseSpan(out int result);

        Assert.IsTrue(ok);
        Assert.AreEqual(42, result);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParseSpan{T}(string, out T)" /> returns
    /// <see langword="false" /> for an invalid input.
    /// </summary>
    [TestMethod]
    public void TryParseSpan_OfInt_WhenInputIsInvalid_ShouldReturnFalse()
    {
        var ok = "not-a-number".TryParseSpan(out int result);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, result);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TryParseSpan{T}(string, out T)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParseSpan_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.TryParseSpan<int>(null!, out _);
        });
    }
}
