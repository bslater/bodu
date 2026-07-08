// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatisticsTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningStatisticsTests
{
    /// <summary>
    /// Verifies that passing the accumulator by value gives the callee an independent copy — the caller does not
    /// observe the callee's additions.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenPassedByValue_ShouldNotObserveCalleeMutation()
    {
        var stats = new RunningStatistics<int>();

        AddOneByValue(stats);

        Assert.AreEqual(0L, stats.Count);
    }

    /// <summary>
    /// Verifies that passing the accumulator by <see langword="ref" /> shares the instance — the caller observes the
    /// callee's additions.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenPassedByRef_ShouldObserveCalleeMutation()
    {
        var stats = new RunningStatistics<int>();

        AddOneByRef(ref stats);

        Assert.AreEqual(1L, stats.Count);
    }

    /// <summary>
    /// Verifies the readonly-field trap: calling <see cref="RunningStatistics{T}.Add" /> through a
    /// <see langword="readonly" /> field mutates a compiler-inserted defensive copy, so the stored accumulator never
    /// changes.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenStoredInReadonlyField_ShouldMutateDefensiveCopyOnly()
    {
        var holder = new ReadonlyFieldHolder();

        holder.AddOneThroughReadonlyField();

        Assert.AreEqual(0L, holder.Stats.Count);
    }

    /// <summary>
    /// Verifies the property trap: calling <see cref="RunningStatistics{T}.Add" /> on a property-returned
    /// accumulator mutates the returned temporary copy, so the stored accumulator never changes.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenMutatedThroughPropertyGetter_ShouldMutateReturnedCopyOnly()
    {
        var holder = new PropertyHolder();

        holder.StatsByValue.Add(1);

        Assert.AreEqual(0L, holder.StatsByValue.Count);
    }

    /// <summary>
    /// Adds a sample to a by-value copy of the accumulator; the caller's instance is unaffected.
    /// </summary>
    /// <param name="stats">The accumulator, received by value.</param>
    private static void AddOneByValue(RunningStatistics<int> stats) =>
        stats.Add(1);

    /// <summary>
    /// Adds a sample to the caller's accumulator through a <see langword="ref" /> parameter.
    /// </summary>
    /// <param name="stats">The accumulator, received by reference.</param>
    private static void AddOneByRef(ref RunningStatistics<int> stats) =>
        stats.Add(1);

    /// <summary>
    /// Holds the accumulator in a <see langword="readonly" /> field to demonstrate the defensive-copy behaviour.
    /// </summary>
    private sealed class ReadonlyFieldHolder
    {
        /// <summary>The accumulator under test; readonly so member calls operate on a defensive copy.</summary>
        public readonly RunningStatistics<int> Stats;

        /// <summary>
        /// Calls <see cref="RunningStatistics{T}.Add" /> through the readonly field.
        /// </summary>
        public void AddOneThroughReadonlyField() =>
            Stats.Add(1);
    }

    /// <summary>
    /// Exposes the accumulator through an auto-property to demonstrate the returned-copy behaviour.
    /// </summary>
    private sealed class PropertyHolder
    {
        /// <summary>Gets the accumulator by value; callers receive a copy.</summary>
        public RunningStatistics<int> StatsByValue { get; } = new();
    }
}
