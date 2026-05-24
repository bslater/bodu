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
    /// Verifies that <see cref="NotableDateService.Dispose" /> releases the internal per-thread tracking field so that
    /// subsequent generation calls fail fast rather than silently consuming a disposed resource.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInvoked_ShouldReleaseInternalThreadLocalState()
    {
        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Base", 6, 1)) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        service.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = service.GetNotableDates(2026);
        });
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
