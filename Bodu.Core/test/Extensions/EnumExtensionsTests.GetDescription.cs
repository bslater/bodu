// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.GetDescription.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EnumExtensions.GetDescription{TEnum}(TEnum)" /> resolves description text with the
    /// documented attribute precedence and name fallback.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <param name="expected">The expected description text.</param>
    [TestMethod]
    [TestCategory("Smoke")]
    [DataRow(DescribedStatus.Active, "Is Active")]
    [DataRow(DescribedStatus.Pending, "Pend")]
    [DataRow(DescribedStatus.Plain, "Plain")]
    [DataRow(DescribedStatus.Cancelled, "Cx")]
    public void GetDescription_WhenQueried_ShouldResolveText(DescribedStatus value, string expected) =>
        Assert.AreEqual(expected, value.GetDescription());

    /// <summary>
    /// Verifies that <see cref="EnumExtensions.GetDisplayName{TEnum}(TEnum)" /> prefers the display name then the
    /// description, falling back to the value's name.
    /// </summary>
    /// <param name="value">The value to name.</param>
    /// <param name="expected">The expected display name.</param>
    [TestMethod]
    [DataRow(DescribedStatus.Active, "Is Active")]
    [DataRow(DescribedStatus.Pending, "Pend")]
    [DataRow(DescribedStatus.Plain, "Plain")]
    [DataRow(DescribedStatus.Cancelled, "Cancelled")]
    public void GetDisplayName_WhenQueried_ShouldResolveName(DescribedStatus value, string expected) =>
        Assert.AreEqual(expected, value.GetDisplayName());

    /// <summary>
    /// Verifies that <see cref="EnumExtensions.TryGetDescription{TEnum}(TEnum, out string)" /> reports whether an
    /// explicit attribute was declared.
    /// </summary>
    [TestMethod]
    public void TryGetDescription_WhenAttributePresentOrAbsent_ShouldReportOutcome()
    {
        Assert.IsTrue(DescribedStatus.Active.TryGetDescription(out var described));
        Assert.AreEqual("Is Active", described);

        Assert.IsFalse(DescribedStatus.Plain.TryGetDescription(out var fallback));
        Assert.AreEqual("Plain", fallback);
    }
}
