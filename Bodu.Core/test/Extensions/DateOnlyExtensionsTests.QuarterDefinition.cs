// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.QuarterDefinition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> when the supplied <see cref="CalendarQuarterDefinition.Custom" /> definition requires a
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> for the Custom definition.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfQuarter_DateOnly_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }

}
