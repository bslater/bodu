// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Contains unit tests for <see cref="NotableDateContext" />.
/// </summary>
[TestClass]
public sealed class NotableDateContextTests
{
    /// <summary>
    /// Reverts the ambient <see cref="NotableDateContext.Default" /> to the lazy fallback after every test so that one test cannot
    /// leak state into another.
    /// </summary>
    [TestCleanup]
    public void Cleanup() => NotableDateContext.Reset();

    /// <summary>
    /// Verifies that reading <see cref="NotableDateContext.Default" /> without prior assignment returns a usable
    /// <see cref="INotableDateService" /> instance backed by the embedded minimal rule set.
    /// </summary>
    [TestMethod]
    public void Default_WhenNotAssigned_ShouldReturnLazyFallbackService()
    {
        INotableDateService service = NotableDateContext.Default;

        Assert.IsNotNull(service);
        Assert.IsInstanceOfType<NotableDateService>(service);
    }

    /// <summary>
    /// Verifies that successive reads of <see cref="NotableDateContext.Default" /> return the same lazy fallback instance.
    /// </summary>
    [TestMethod]
    public void Default_WhenReadRepeatedly_ShouldReturnSameLazyFallbackInstance()
    {
        INotableDateService first = NotableDateContext.Default;
        INotableDateService second = NotableDateContext.Default;

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that assigning <see cref="NotableDateContext.Default" /> to a custom service replaces the value returned by the
    /// getter on subsequent reads.
    /// </summary>
    [TestMethod]
    public void Default_WhenAssignedCustomService_ShouldReturnAssignedInstance()
    {
        INotableDateService configured = new NotableDateService();
        NotableDateContext.Default = configured;

        Assert.AreSame(configured, NotableDateContext.Default);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> reverts <see cref="NotableDateContext.Default" /> to the lazy fallback.
    /// </summary>
    [TestMethod]
    public void Default_WhenAssignedNull_ShouldRevertToLazyFallback()
    {
        INotableDateService configured = new NotableDateService();
        NotableDateContext.Default = configured;

        NotableDateContext.Default = null!;

        Assert.IsNotNull(NotableDateContext.Default);
        Assert.AreNotSame(configured, NotableDateContext.Default);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateContext.Reset" /> discards a previously assigned service so that subsequent reads return
    /// the lazy fallback.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalled_ShouldRestoreLazyFallback()
    {
        INotableDateService configured = new NotableDateService();
        NotableDateContext.Default = configured;

        NotableDateContext.Reset();

        Assert.AreNotSame(configured, NotableDateContext.Default);
    }
}
