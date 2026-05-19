// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniEntryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Behavioural tests for <see cref="IniEntry" />.
/// </summary>
[TestClass]
public sealed class IniEntryTests
{

    /// <summary>
    /// Verifies that <see cref="IniEntry.Key" /> and <see cref="IniEntry.Value" /> expose the values supplied at
    /// construction.
    /// </summary>
    [TestMethod]
    public void Construction_WhenSuppliedKeyAndValue_ShouldExposeProperties()
    {
        IniEntry entry = new("host", "localhost");

        Assert.AreEqual("host", entry.Key);
        Assert.AreEqual("localhost", entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> parses an integer value string correctly.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsValidInt_ShouldReturnParsedInt()
    {
        IniEntry entry = new("port", "8080");

        int result = entry.GetValue<int>();

        Assert.AreEqual(8080, result);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> parses a double value string using
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsValidDouble_ShouldReturnParsedDouble()
    {
        IniEntry entry = new("ratio", "3.14");

        double result = entry.GetValue<double>();

        Assert.AreEqual(3.14, result, delta: 1e-10);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> parses a boolean value string.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsValidBool_ShouldReturnParsedBool()
    {
        IniEntry entry = new("enabled", "true");

        bool result = entry.GetValue<bool>();

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> returns the raw string when the target type is
    /// <see cref="string" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenTargetTypeIsString_ShouldReturnRawValue()
    {
        IniEntry entry = new("name", "Alice");

        string result = entry.GetValue<string>();

        Assert.AreEqual("Alice", result);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> parses a <see cref="Guid" /> value string.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsValidGuid_ShouldReturnParsedGuid()
    {
        Guid expected = Guid.NewGuid();
        IniEntry entry = new("id", expected.ToString());

        Guid result = entry.GetValue<Guid>();

        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.GetValue{T}" /> throws <see cref="FormatException" /> when the value
    /// string cannot be parsed as the target type.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsInvalidForTargetType_ShouldThrowFormatException()
    {
        IniEntry entry = new("port", "not-a-number");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = entry.GetValue<int>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.TryGetValue{T}" /> returns <see langword="true" /> and sets the output
    /// when the value parses successfully.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsValidInt_ShouldReturnTrueAndSetOutput()
    {
        IniEntry entry = new("count", "7");

        bool result = entry.TryGetValue<int>(out int value);

        Assert.IsTrue(result);
        Assert.AreEqual(7, value);
    }

    /// <summary>
    /// Verifies that <see cref="IniEntry.TryGetValue{T}" /> returns <see langword="false" /> and sets the output
    /// to the default when the value cannot be parsed.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsInvalidForTargetType_ShouldReturnFalse()
    {
        IniEntry entry = new("count", "abc");

        bool result = entry.TryGetValue<int>(out int value);

        Assert.IsFalse(result);
        Assert.AreEqual(default, value);
    }

}
