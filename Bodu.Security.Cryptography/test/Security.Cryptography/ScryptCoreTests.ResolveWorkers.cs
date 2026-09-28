// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.ResolveWorkers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>The size of a 16 MiB <c>V</c>, as at <c>N = 16384</c> and <c>r = 8</c>.</summary>
    private const long SixteenMiB = 16L << 20;

    /// <summary>
    /// Verifies that a bound of one keeps every unit on the calling thread.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenBoundIsOne_ShouldReturnOne()
    {
        var options = new ScryptCore.MixOptions(1);

        Assert.AreEqual(1, options.ResolveWorkers(16, SixteenMiB));
    }

    /// <summary>
    /// Verifies that a derivation with a single unit stays on the calling thread whatever the bound.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenThereIsOneUnit_ShouldReturnOne()
    {
        var options = new ScryptCore.MixOptions(-1);

        Assert.AreEqual(1, options.ResolveWorkers(1, SixteenMiB));
    }

    /// <summary>
    /// Verifies that units smaller than the threshold stay on the calling thread, and units at the threshold do not.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenUnitsAreBelowTheThreshold_ShouldReturnOne()
    {
        var options = new ScryptCore.MixOptions(4);

        Assert.AreEqual(1, options.ResolveWorkers(16, ScryptCore.MixOptions.DefaultMinimumParallelUnitBytes - 1));
        Assert.AreEqual(4, options.ResolveWorkers(16, ScryptCore.MixOptions.DefaultMinimumParallelUnitBytes));
    }

    /// <summary>
    /// Verifies that a bound caps the threads below the number of units, and the number of units caps them below the
    /// bound.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenBounded_ShouldReturnTheSmallerOfBoundAndUnits()
    {
        Assert.AreEqual(3, new ScryptCore.MixOptions(3).ResolveWorkers(16, SixteenMiB));
        Assert.AreEqual(2, new ScryptCore.MixOptions(8).ResolveWorkers(2, SixteenMiB));
    }

    /// <summary>
    /// Verifies that a bound of <c>-1</c> allows one thread per processor, capped by the number of units.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenBoundIsMinusOne_ShouldAllowOneThreadPerProcessor()
    {
        var options = new ScryptCore.MixOptions(-1);

        Assert.AreEqual(Math.Min(Environment.ProcessorCount, 64), options.ResolveWorkers(64, SixteenMiB));
    }

    /// <summary>
    /// Verifies that the threads are capped so that their <c>V</c>s together stay within the ceiling on a single one,
    /// however high the bound: two at 1 GiB each, one at 2 GiB.
    /// </summary>
    [TestMethod]
    public void ResolveWorkers_WhenUnitsAreLarge_ShouldKeepTotalMemoryWithinTheCeiling()
    {
        var options = new ScryptCore.MixOptions(16);

        Assert.AreEqual(2, options.ResolveWorkers(16, ScryptParameters.MaxMemoryBytes / 2));
        Assert.AreEqual(1, options.ResolveWorkers(16, ScryptParameters.MaxMemoryBytes));
    }
}
