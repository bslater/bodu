// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlFuzzCorpusReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Reads the committed fuzz-crash corpus — minimized inputs that SharpFuzz found, each carrying the contract the
/// parser must honor for it (<c>RoundTrips</c> or <c>Throws</c>) — into <see cref="YamlTestVector" /> records.
/// </summary>
/// <remarks>
/// Each vector mirrors the conformance-corpus layout (a directory with <c>===</c> and <c>in.yaml</c>); a flat
/// <c>classification.tsv</c> (id, kind, expectation) records the expected outcome. The corpus is copied to the test
/// output by the project's <c>YamlFuzzCorpus\**</c> glob.
/// </remarks>
internal static class YamlFuzzCorpusReader
{
    /// <summary>
    /// The root directory of the fuzz-crash corpus within the test output.
    /// </summary>
    internal static readonly string CorpusRoot = Path.Combine(AppContext.BaseDirectory, "YamlFuzzCorpus");

    /// <summary>
    /// Reads the classification manifest rows.
    /// </summary>
    /// <returns>The manifest rows as identifier, kind, and expectation triples.</returns>
    internal static IEnumerable<(string Id, string Kind, string Expectation)> ReadManifest()
    {
        foreach (var line in File.ReadLines(Path.Combine(CorpusRoot, "classification.tsv")))
        {
            if (line.Length == 0)
                continue;

            var parts = line.Split('\t');
            yield return (parts[0], parts[1], parts[2]);
        }
    }

    /// <summary>
    /// Loads every vector with the requested expectation into a <see cref="YamlTestVector" />.
    /// </summary>
    /// <param name="expectation">The expectation to select (<c>RoundTrips</c> or <c>Throws</c>).</param>
    /// <returns>One vector per matching case.</returns>
    internal static IEnumerable<YamlTestVector> LoadByExpectation(string expectation)
    {
        foreach (var (id, kind, exp) in ReadManifest())
        {
            if (!string.Equals(exp, expectation, StringComparison.Ordinal))
                continue;

            var dir = Path.Combine(CorpusRoot, id);
            var descriptionPath = Path.Combine(dir, "===");
            var description = File.Exists(descriptionPath) ? File.ReadAllText(descriptionPath).Trim() : string.Empty;

            yield return new YamlTestVector
            {
                Id = id,
                Description = description,
                Kind = kind,
                Category = exp,
                Yaml = File.ReadAllBytes(Path.Combine(dir, "in.yaml")),
            };
        }
    }
}
