// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the disposal contract of <see cref="NotableDateService" />.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateService.Dispose" /> may be called and is idempotent — calling it twice does
    /// not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInvokedTwice_ShouldNotThrow()
    {
        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 6, 1))],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        service.Dispose();
        service.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.Dispose" /> is safe to call before any query has been issued — the
    /// internal lazy-initialised state still releases cleanly.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInvokedOnNewService_ShouldNotThrow()
    {
        NotableDateService service = new(
            ruleProviders: Array.Empty<INotableDateRuleProvider>(),
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        service.Dispose();
    }
}
