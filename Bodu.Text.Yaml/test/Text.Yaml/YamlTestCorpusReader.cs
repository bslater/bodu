// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Reads the <c>yaml/yaml-test-suite</c> corpus — linked into the repository as the <c>yaml-test-suite</c> git
/// submodule — joining each upstream vector directory with the repository's own <c>classification.tsv</c> to produce a
/// <see cref="YamlTestVector" /> the conformance run can execute.
/// </summary>
/// <remarks>
/// The submodule mirrors the upstream layout (a directory per vector containing <c>===</c>, <c>in.yaml</c>, and the
/// optional <c>in.json</c> / <c>error</c> / <c>test.event</c> / <c>out.yaml</c> / <c>emit.yaml</c> files). The submodule
/// vectors and the Bodu classification manifest are copied to the test output by the project's <c>None</c> globs; run
/// <c>git submodule update --init</c> to populate the submodule before the Regression tier.
/// </remarks>
internal static class YamlTestCorpusReader
{
    /// <summary>
    /// The root directory of the yaml-test-suite vectors (the submodule) within the test output.
    /// </summary>
    internal static readonly string VectorRoot = Path.Combine(AppContext.BaseDirectory, "yaml-test-suite");

    /// <summary>
    /// The directory holding the Bodu profile classification manifest within the test output.
    /// </summary>
    internal static readonly string ManifestRoot = Path.Combine(AppContext.BaseDirectory, "YamlTestCorpus");

    /// <summary>
    /// Reads the classification manifest rows.
    /// </summary>
    /// <returns>The manifest rows as identifier, suite kind, and classification triples.</returns>
    internal static IEnumerable<(string Id, string Kind, string Category)> ReadManifest()
    {
        foreach (var line in File.ReadLines(Path.Combine(ManifestRoot, "classification.tsv")))
        {
            if (line.Length == 0)
                continue;

            var parts = line.Split('\t');
            yield return (parts[0], parts[1], parts[2]);
        }
    }

    /// <summary>
    /// Loads every classified vector in the requested category into a <see cref="YamlTestVector" />.
    /// </summary>
    /// <param name="category">The classification category to select.</param>
    /// <returns>One vector per classified case in the category.</returns>
    internal static IEnumerable<YamlTestVector> LoadByCategory(string category)
    {
        foreach (var (id, kind, cat) in ReadManifest())
        {
            if (string.Equals(cat, category, StringComparison.Ordinal))
                yield return Load(id, kind, cat);
        }
    }

    /// <summary>
    /// Returns the absolute directory of a vector within the submodule.
    /// </summary>
    /// <param name="id">The vector identifier (its path relative to the corpus root).</param>
    /// <returns>The vector directory path.</returns>
    internal static string VectorDirectory(string id) =>
        Path.Combine(VectorRoot, id.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Loads a single vector by identifier, reading its description, input, and expectation files.
    /// </summary>
    /// <param name="id">The vector identifier (its path relative to the corpus root).</param>
    /// <param name="kind">The suite kind from the manifest.</param>
    /// <param name="category">The classification category from the manifest.</param>
    /// <returns>The loaded vector.</returns>
    internal static YamlTestVector Load(string id, string kind, string category)
    {
        var dir = VectorDirectory(id);

        var descriptionPath = Path.Combine(dir, "===");
        var description = File.Exists(descriptionPath) ? File.ReadAllText(descriptionPath).Trim() : string.Empty;

        var jsonPath = Path.Combine(dir, "in.json");
        var json = File.Exists(jsonPath) ? File.ReadAllBytes(jsonPath) : null;

        return new YamlTestVector
        {
            Id = id,
            Description = description,
            Kind = kind,
            Category = category,
            Yaml = File.ReadAllBytes(Path.Combine(dir, "in.yaml")),
            Json = json,
        };
    }
}
