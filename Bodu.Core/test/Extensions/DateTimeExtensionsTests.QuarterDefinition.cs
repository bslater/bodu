// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.QuarterDefinition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> when the supplied <see cref="CalendarQuarterDefinition.Custom" /> definition requires a
    /// provider-based overload.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfQuarter_DateTime_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfQuarter(int, int, CalendarQuarterDefinition)" /> throws
    /// <see cref="InvalidOperationException" /> for the Custom definition.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfQuarter_DateTime_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfQuarter(2024, 1, CalendarQuarterDefinition.Custom);
        });
    }
}
