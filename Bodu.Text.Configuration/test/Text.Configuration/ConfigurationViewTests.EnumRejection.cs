// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.EnumRejection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    private enum RejectionSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    [Flags]
    private enum RejectionPermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
    }

    /// <summary>
    /// Verifies that a numeric value corresponding to a defined enum member is accepted by
    /// <see cref="ConfigurationView.GetEnum{TEnum}(string)" />. This is the BCL default and the contract
    /// the implementation inherits from <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)" />.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueIsNumericAndDefined_ShouldReturnEnumMember()
    {
        IniDocument doc = ConfigurationDocument.Parse("[*]\nseverity = 1\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(RejectionSeverity.Warning, view.GetEnum<RejectionSeverity>("severity"));
    }

    /// <summary>
    /// Verifies that a numeric value with no matching enum member is rejected. The implementation guards
    /// with <see cref="Enum.IsDefined(System.Type, object)" /> so undefined integers do not produce
    /// synthetic enum values.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueIsNumericButUndefined_ShouldThrowFormatException()
    {
        IniDocument doc = ConfigurationDocument.Parse("[*]\nseverity = 99\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum<RejectionSeverity>("severity"));
    }

    /// <summary>
    /// Verifies that a negative numeric value is rejected when no matching enum member exists.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenValueIsNegativeAndUndefined_ShouldThrowFormatException()
    {
        IniDocument doc = ConfigurationDocument.Parse("[*]\nseverity = -1\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum<RejectionSeverity>("severity"));
    }

    /// <summary>
    /// Verifies that comma-separated Flags input is <em>rejected</em> when the combined value is not itself
    /// a declared member. The implementation's <see cref="Enum.IsDefined(System.Type, object)" /> guard
    /// treats <c>Read, Write</c> as undefined; callers who need combined-flag parsing should call
    /// <see cref="Enum.Parse(System.Type, string, bool)" /> against
    /// <see cref="ConfigurationView.GetString(string)" /> instead.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenFlagsEnumWithCombinedValue_ShouldRejectAsUndefined()
    {
        IniDocument doc = ConfigurationDocument.Parse("[*]\npermissions = Read, Write\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<FormatException>(() => _ = view.GetEnum<RejectionPermissions>("permissions"));
    }

    /// <summary>
    /// Verifies that a single Flags value matching a declared member <em>is</em> accepted — the rejection
    /// rule above is specifically about combined-value names, not single members of a Flags enum.
    /// </summary>
    [TestMethod]
    public void GetEnum_WhenFlagsEnumWithSingleValue_ShouldReturnMember()
    {
        IniDocument doc = ConfigurationDocument.Parse("[*]\npermissions = Read\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.AreEqual(RejectionPermissions.Read, view.GetEnum<RejectionPermissions>("permissions"));
    }
}
