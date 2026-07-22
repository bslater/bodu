// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.GetEnum.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    private enum Severity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Verifies that an enum value matching a defined member is parsed case-insensitively.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueMatchesEnumMember_ShouldReturnParsedValue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nseverity = warning\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(Severity.Warning, view.GetEnum<Severity>("severity"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEnum{TEnum}(string)" /> throws
    /// <see cref="KeyNotFoundException" /> for absent keys.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenKeyIsMissing_ShouldThrowExactly()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = view.GetEnum<Severity>("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEnum{TEnum}(string)" /> throws
    /// <see cref="FormatException" /> when the value does not match any enum member.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueDoesNotMatchEnumMember_ShouldThrowExactly()
    {
        var doc = ConfigurationDocument.Parse("[*]\nseverity = catastrophic\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum<Severity>("severity"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEnum{TEnum}(string, TEnum)" /> returns the fallback only when the
    /// key is absent.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenKeyMissingAndFallbackProvided_ShouldReturnFallback()
    {
        IniDocument doc = new();
        ConfigurationView view = doc.Resolve();

        Assert.AreEqual(Severity.Error, view.GetEnum("missing", Severity.Error));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEnum{TEnum}(string, TEnum)" /> still throws
    /// <see cref="FormatException" /> when the key is present but does not match any enum member, even with a fallback.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenPresentButMalformedAndFallbackProvided_ShouldThrowExactly()
    {
        var doc = ConfigurationDocument.Parse("[*]\nseverity = catastrophic\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum("severity", Severity.Info));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.TryGetEnum{TEnum}(string, out TEnum)" /> reports failure for missing
    /// or undefined values without throwing, and success for a defined member.
    /// </summary>
    [TestMethod]
    public void TryGetEnum_WhenMissingOrUndefined_ShouldReturnFalseOtherwiseTrue()
    {
        var doc = ConfigurationDocument.Parse("[*]\nseverity = warning\nbad = catastrophic\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.TryGetEnum<Severity>("severity", out Severity parsed));
        Assert.AreEqual(Severity.Warning, parsed);
        Assert.IsFalse(view.TryGetEnum<Severity>("missing", out _));
        Assert.IsFalse(view.TryGetEnum<Severity>("bad", out _));
    }
}
