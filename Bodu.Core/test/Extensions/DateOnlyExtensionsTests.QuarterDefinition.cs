// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.QuarterDefinition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfQuarter(CalendarQuarterDefinition, int, int)" /> throws
    /// <see cref="InvalidOperationException" /> when the supplied <see cref="CalendarQuarterDefinition.Custom" /> definition requires a
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfQuarter(CalendarQuarterDefinition.Custom, 1, 2024);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfQuarter(CalendarQuarterDefinition, int, int)" /> throws
    /// <see cref="InvalidOperationException" /> for the Custom definition.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfQuarter(CalendarQuarterDefinition.Custom, 1, 2024);
        });
    }
}
