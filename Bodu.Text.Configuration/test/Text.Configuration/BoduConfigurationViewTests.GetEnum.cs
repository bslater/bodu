// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationViewTests.GetEnum.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationViewTests
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
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\nseverity = warning\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(Severity.Warning, view.GetEnum<Severity>("severity"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetEnum{TEnum}(string)" /> throws
    /// <see cref="KeyNotFoundException" /> for absent keys.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenKeyIsMissing_ShouldThrowExactly()
    {
        IniDocument doc = new();
        BoduConfigurationView view = doc.Resolve();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = view.GetEnum<Severity>("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationView.GetEnum{TEnum}(string)" /> throws
    /// <see cref="FormatException" /> when the value does not match any enum member.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueDoesNotMatchEnumMember_ShouldThrowExactly()
    {
        IniDocument doc = BoduConfigurationDocument.Parse("[*]\nseverity = catastrophic\n");
        BoduConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum<Severity>("severity"));
    }
}
