// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlFuzzCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Test.Kat;

namespace Bodu.Text.Yaml;

/// <summary>
/// Replays the minimized inputs SharpFuzz found against the parser, asserting the contract each one must honor: an
/// input marked <c>RoundTrips</c> must survive a parse-serialize-reparse cycle unchanged, and one marked <c>Throws</c>
/// must be rejected with <see cref="YamlFormatException" /> rather than crashing or hanging. These are the permanent
/// regression guards for the fuzzing findings.
/// </summary>
[TestClass]
public sealed class YamlFuzzCorpusTests
{
    /// <summary>
    /// Yields the fuzz vectors that must round-trip unchanged.
    /// </summary>
    /// <returns>One row per round-trip vector.</returns>
    public static IEnumerable<object[]> RoundTripCases() => VectorsFor("RoundTrips");

    /// <summary>
    /// Verifies that a fuzz round-trip vector parses, re-serializes, and reparses to the same value graph.
    /// </summary>
    /// <param name="kat">The fuzz vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(RoundTripCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void RoundTrip_WhenFuzzVector_ShouldRemainStable(YamlTestVector kat)
    {
        var first = YamlSerializer.Deserialize<object>(kat.Yaml);
        var reemitted = YamlSerializer.Serialize(first);
        var second = YamlSerializer.Deserialize<object>(reemitted);

        Assert.AreEqual(
            JsonSerializer.Serialize(first),
            JsonSerializer.Serialize(second),
            $"{kat.Id}\n--- re-emitted ---\n{reemitted}");
    }

    // A `Throws` runner over LoadByExpectation("Throws") is added the first time fuzzing finds an input that must be
    // rejected; the crash/DoS hunt that produced this corpus found none (the parser did not crash or hang), so the
    // corpus currently holds only round-trip findings.

    /// <summary>
    /// Verifies that the vendored fuzz corpus count matches its pinned <c>provenance.json</c>, so findings are not
    /// silently dropped.
    /// </summary>
    [TestMethod]
    public void Corpus_MatchesPinnedProvenance()
    {
        using var provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(YamlFuzzCorpusReader.CorpusRoot, "provenance.json")));

        var expected = provenance.RootElement.GetProperty("caseCount").GetInt32();
        var actual = YamlFuzzCorpusReader.ReadManifest().Count();

        Assert.AreEqual(expected, actual, "The fuzz corpus count drifted from the pinned provenance.");
    }

    /// <summary>
    /// Loads the vectors with the requested expectation as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="expectation">The expectation to select.</param>
    /// <returns>One row per matching vector.</returns>
    private static IEnumerable<object[]> VectorsFor(string expectation)
    {
        foreach (var vector in YamlFuzzCorpusReader.LoadByExpectation(expectation))
            yield return [vector];
    }
}
