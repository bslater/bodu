// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvEntryTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Unit tests for <see cref="DotEnvEntry" />.
/// </summary>
[TestClass]
public sealed class DotEnvEntryTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> parses the value as <see cref="int" /> correctly.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsInt_ShouldReturnParsedInt()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=8080");

        var port = doc.Entries[0].GetValue<int>();

        Assert.AreEqual(8080, port);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> parses the value as <see cref="double" /> correctly.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsDouble_ShouldReturnParsedDouble()
    {
        DotEnvDocument doc = DotEnv.Parse("RATIO=3.14");

        var ratio = doc.Entries[0].GetValue<double>();

        Assert.AreEqual(3.14, ratio, 1e-10);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> parses the value as <see cref="bool" /> correctly.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsBool_ShouldReturnParsedBool()
    {
        DotEnvDocument doc = DotEnv.Parse("DEBUG=True");

        var debug = doc.Entries[0].GetValue<bool>();

        Assert.IsTrue(debug);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> parses the value as <see cref="Guid" /> correctly.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsGuid_ShouldReturnParsedGuid()
    {
        var expected = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        DotEnvDocument doc = DotEnv.Parse($"ID={expected}");

        Guid actual = doc.Entries[0].GetValue<Guid>();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> for <see cref="string" /> returns the value
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsString_ShouldReturnSameText()
    {
        DotEnvDocument doc = DotEnv.Parse("NAME=hello");

        var name = doc.Entries[0].GetValue<string>();

        Assert.AreEqual("hello", name);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.GetValue{T}" /> throws <see cref="FormatException" /> when the value
    /// cannot be parsed as the requested type.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsNotParseable_ShouldThrowExactly()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=notanumber");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = doc.Entries[0].GetValue<int>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.TryGetValue{T}" /> returns <see langword="true" /> and the parsed
    /// value when parsing succeeds.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsParseable_ShouldReturnTrueAndValue()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=9000");

        var result = doc.Entries[0].TryGetValue<int>(out var port);

        Assert.IsTrue(result);
        Assert.AreEqual(9000, port);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvEntry.TryGetValue{T}" /> returns <see langword="false" /> when parsing
    /// fails.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsNotParseable_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=notanumber");

        var result = doc.Entries[0].TryGetValue<int>(out var _);

        Assert.IsFalse(result);
    }

}
