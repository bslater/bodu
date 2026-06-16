// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.Getters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Builds a resolved view carrying one value of each typed shape used by the getter tests.
    /// </summary>
    /// <returns>The resolved view.</returns>
    private static ConfigurationView TypedView() =>
        ConfigurationDocument
            .Parse("[*]\nint = 42\nlong = 99\nflag = true\nday = Monday\ngeneric = 7\n")
            .Resolve("any.cs");

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetValue{T}(string, out T)" /> succeeds for a present, parseable
    /// value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenPresentAndParseable_ShouldReturnTrueAndValue()
    {
        Assert.IsTrue(TypedView().TryGetValue<int>("generic", out int value));
        Assert.AreEqual(7, value);
    }

    /// <summary>
    /// Verifies that the fallback overload of <see cref="ConfigurationView.GetInt32(string, int)" /> returns the parsed
    /// value when the key is present.
    /// </summary>
    [TestMethod]
    public void GetInt32_WithFallback_WhenPresent_ShouldReturnParsedValue()
    {
        Assert.AreEqual(42, TypedView().GetInt32("int", -1));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetInt32(string, out int)" /> succeeds for a present value.
    /// </summary>
    [TestMethod]
    public void TryGetInt32_WhenPresent_ShouldReturnTrueAndValue()
    {
        Assert.IsTrue(TypedView().TryGetInt32("int", out int value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that the fallback overload of <see cref="ConfigurationView.GetInt64(string, long)" /> returns the parsed
    /// value when the key is present.
    /// </summary>
    [TestMethod]
    public void GetInt64_WithFallback_WhenPresent_ShouldReturnParsedValue()
    {
        Assert.AreEqual(99L, TypedView().GetInt64("long", -1L));
    }

    /// <summary>
    /// Verifies that the fallback overload of <see cref="ConfigurationView.GetBoolean(string, bool)" /> returns the
    /// parsed value when the key is present.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WithFallback_WhenPresent_ShouldReturnParsedValue()
    {
        Assert.IsTrue(TypedView().GetBoolean("flag", false));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetBoolean(string, out bool)" /> succeeds for a present value.
    /// </summary>
    [TestMethod]
    public void TryGetBoolean_WhenPresent_ShouldReturnTrueAndValue()
    {
        Assert.IsTrue(TypedView().TryGetBoolean("flag", out bool value));
        Assert.IsTrue(value);
    }

    /// <summary>
    /// Verifies that the fallback overload of <see cref="ConfigurationView.GetEnum{TEnum}(string, TEnum)" /> returns the
    /// parsed value when the key is present.
    /// </summary>
    [TestMethod]
    public void GetEnum_WithFallback_WhenPresent_ShouldReturnParsedValue()
    {
        Assert.AreEqual(DayOfWeek.Monday, TypedView().GetEnum("day", DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that the fallback overload of <see cref="ConfigurationView.GetBoolean(string, bool)" /> still throws
    /// <see cref="FormatException" /> when the present value is malformed.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WithFallback_WhenPresentValueMalformed_ShouldThrowFormatException()
    {
        ConfigurationView view = TypedView();

        Assert.ThrowsExactly<FormatException>(() => view.GetBoolean("int", false));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetInt64(string)" /> throws <see cref="FormatException" /> when the
    /// present value cannot be parsed as a 64-bit integer.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenValueMalformed_ShouldThrowFormatException()
    {
        ConfigurationView view = TypedView();

        Assert.ThrowsExactly<FormatException>(() => view.GetInt64("flag"));
    }
}
