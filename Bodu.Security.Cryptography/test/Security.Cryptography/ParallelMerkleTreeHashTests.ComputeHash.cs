// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="ParallelMerkleTreeHash.ComputeHash(System.ReadOnlySpan{byte})" />
/// scenarios that are <b>not</b> shared with the sequential implementation — specifically,
/// diagnostics capture on a per-call basis and concurrent factory invocation by the worker
/// pool.
/// </summary>
/// <remarks>
/// Shared <c>ComputeHash</c> tests (argument validation, determinism, overload equivalence,
/// vectors, boundaries, reuse, tree shapes, immutability, etc.) are inherited from
/// <see cref="MerkleTreeHashTestsBase{THasher}" /> — see the base class for the complete list.
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    // ─── Per-call diagnostics capture ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that attaching a fresh <see cref="MerkleTreeDiagnostics" /> on each call records
    /// only the nodes from that particular call — state must not accumulate across invocations.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenDiagnosticsPassedPerCall_ShouldRecordOnlyThatCallsNodes()
    {
        var data = MakeData(8); // 2 full blocks → 2 leaves + 1 internal = 3 nodes

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

        var diag1 = new MerkleTreeDiagnostics();
        var diag2 = new MerkleTreeDiagnostics();

        hasher.ComputeHash(data, diag1);
        hasher.ComputeHash(data, diag2);

        Assert.AreEqual(3, diag1.GetAllNodes().Count,
            "First diagnostics should contain 2 leaves + 1 internal = 3 nodes.");
        Assert.AreEqual(3, diag2.GetAllNodes().Count,
            "Second diagnostics should contain 3 nodes, not 6 — call state must not accumulate.");
    }

    /// <summary>
    /// Verifies that omitting diagnostics on one call and then supplying them on the next
    /// produces a clean, isolated trace containing only the second call's nodes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenDiagnosticsOmittedThenSupplied_ShouldOnlyRecordSecondCall()
    {
        var data = MakeData(8);

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

        hasher.ComputeHash(data); // no diagnostics

        var diag = new MerkleTreeDiagnostics();
        hasher.ComputeHash(data, diag); // diagnostics attached

        Assert.AreEqual(3, diag.GetAllNodes().Count,
            "Diagnostics should contain only the second call's 3 nodes.");
    }

    // ─── Worker-pool factory concurrency ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that concurrent level workers each receive a distinct factory-produced
    /// <see cref="HashAlgorithm" /> instance — confirms a simple <c>new T()</c> factory is
    /// sufficient and that the implementation imposes no additional thread-safety
    /// requirements on the factory beyond re-entrancy.
    /// </summary>
    /// <remarks>
    /// 16 leaves with <c>fanOut=2</c> produce 15 internal nodes for a total of 31 algorithm
    /// instances — enough that any accidental sharing would be statistically detectable.
    /// </remarks>
    [TestMethod]
    public void ComputeHash_WhenFactoryInvokedConcurrently_ShouldReceiveDistinctInstances()
    {
        var instances = new ConcurrentBag<HashAlgorithm>();

        System.Func<HashAlgorithm> trackingFactory = () =>
        {
            var algo = new MonitoringHashAlgorithm();
            instances.Add(algo);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(trackingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(64));

        var list = instances.ToList();
        Assert.AreEqual(
            list.Count,
            list.Distinct(System.Collections.Generic.ReferenceEqualityComparer.Instance).Count(),
            "Factory returned a shared instance — concurrent level workers must receive distinct objects.");
    }
}
