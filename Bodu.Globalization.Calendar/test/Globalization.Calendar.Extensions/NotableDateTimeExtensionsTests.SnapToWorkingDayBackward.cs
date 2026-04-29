// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.SnapToWorkingDayBackward.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a working day is returned unchanged.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenInputIsWorkingDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateTime input = new DateTime(2026, 1, 6);

        DateTime result = input.SnapToWorkingDayBackward(service);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that a Sunday snaps backward to the prior Friday.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenInputIsSunday_ShouldSnapToPriorFriday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 4).SnapToWorkingDayBackward(service);

        Assert.AreEqual(new DateTime(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateTime result = new DateTime(2026, 1, 4).SnapToWorkingDayBackward();

            Assert.AreEqual(new DateTime(2026, 1, 2), result);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void SnapToWorkingDayBackward_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 4).SnapToWorkingDayBackward(service: null!);
        });
    }
}
