// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TestCategories.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test;

/// <summary>
/// Provides the standard <c>TestCategory</c> constants used to partition the test suite into tiered runs (smoke,
/// BVT, regression, stress) so that build pipelines can execute a fast subset on every commit and the full set on
/// demand.
/// </summary>
/// <remarks>
/// <para>
/// The tiers form an inclusive hierarchy:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="Smoke" /> — the smallest possible run that exercises one primary happy-path on each public
///       type. It is intended to detect catastrophic breakage and is run by <c>smoke.runsettings</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Regression" /> — tests omitted from the default build run because they are exhaustive,
///       parameterized over published vectors, or otherwise duplicate coverage already provided by structural
///       tests. The default <c>bvt.runsettings</c> filters these out; <c>regression.runsettings</c> includes
///       everything.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Stress" /> — long-running, high-iteration tests (existing convention). Excluded from BVT;
///       included in regression runs.
///     </description>
///   </item>
/// </list>
/// <para>
/// A test method that carries no category attribute is treated as part of the BVT tier — the default build run
/// executes every uncategorised test plus everything in <see cref="Smoke" />.
/// </para>
/// </remarks>
public static class TestCategories
{
    /// <summary>
    /// Identifies the smallest "did anything obviously break" run. Apply to one happy-path test per primary
    /// production type so the smoke suite covers every key entry point.
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Identifies tests that are excluded from the default build/BVT run because they are exhaustive (full
    /// known-answer vectors, full algorithm catalogues, large parameter sweeps, multi-decade calendar tables).
    /// They run in the full regression suite.
    /// </summary>
    public const string Regression = "Regression";

    /// <summary>
    /// Identifies long-running stress tests that hammer a type with many iterations or large workloads. Excluded
    /// from BVT; included in regression runs.
    /// </summary>
    public const string Stress = "Stress";
}
