// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DurableBackendsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Verifies that the <see cref="DurableBackends" /> scenario cleans up after itself on every platform: it disposes
/// the SQLite caches it creates, so deleting their database file at the end succeeds and leaves nothing open.
/// </summary>
[TestClass]
public sealed class DurableBackendsTests
{
    /// <summary>
    /// Verifies that the scenario releases its SQLite database file before it returns. On Windows a leaked handle
    /// makes the scenario's own deletion of the file throw; on Linux the deletion succeeds, so the test reads the
    /// process's open files instead.
    /// </summary>
    [TestMethod]
    public void Run_WhenItReturns_ShouldHoldNoHandleOnItsDatabaseFile()
    {
        if (!OperatingSystem.IsWindows() && !OpenFileHandles.IsSupported)
            Assert.Inconclusive("Needs Windows, where deleting an open file fails, or /proc/self/fd (Linux).");

        DurableBackends.Run();

        if (OpenFileHandles.IsSupported)
        {
            IReadOnlyList<string> held = OpenFileHandles.Find("bodu-calendar-cache-");
            Assert.IsEmpty(held, "Still open: " + string.Join(", ", held));
        }
    }
}
