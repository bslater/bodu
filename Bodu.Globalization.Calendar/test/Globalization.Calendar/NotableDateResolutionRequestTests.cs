// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionRequestTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Tests the <see cref="NotableDateResolutionRequest" /> class.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionRequestTests
{
    /// <summary>
    /// Verifies that request dates are normalised to their date component.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDateTimesContainTimeComponents_ShouldNormalizeToDates()
    {
        NotableDateResolutionRequest request = new(
            new DateTime(2024, 2, 10, 13, 30, 00),
            new DateTime(2024, 2, 20, 23, 59, 59),
            NotableDateResolutionProjection.ObservedDate,
            territoryCode: "AU",
            calendarType: typeof(System.Globalization.GregorianCalendar));

        Assert.AreEqual(new DateTime(2024, 2, 10), request.StartDate);
        Assert.AreEqual(new DateTime(2024, 2, 20), request.EndDate);
        Assert.AreEqual(NotableDateResolutionProjection.ObservedDate, request.Projection);
        Assert.AreEqual("AU", request.TerritoryCode);
        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), request.CalendarType);
        Assert.IsNull(request.Filter);
    }

    /// <summary>
    /// Verifies that invalid request windows are rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEndDateIsEarlierThanStartDate_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NotableDateResolutionRequest(
                new DateTime(2024, 2, 20),
                new DateTime(2024, 2, 10),
                NotableDateResolutionProjection.ObservedDate);
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }
}