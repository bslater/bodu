// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Reads the <c>yaml/yaml-test-suite</c> corpus — mirrored into the repository as the <c>yaml-test-suite</c> git
/// submodule — by walking its directory structure and classifying each case against the Bodu YAML Core Tree Profile in
/// code, producing a <see cref="YamlTestVector" /> the conformance run can execute.
/// </summary>
/// <remarks>
/// <para>
/// The submodule mirrors the upstream layout (a directory per case containing <c>===</c>, <c>in.yaml</c>, and the
/// optional <c>in.json</c> / <c>error</c> / <c>test.event</c> / <c>out.yaml</c> / <c>emit.yaml</c> files). Run
/// <c>git submodule update --init</c> to populate it, and the project's <c>None</c> glob copies it to the test output.
/// </para>
/// <para>
/// Classification is derived from the case's own files — a case carrying an <c>error</c> file is one the suite expects
/// to fail (<c>SupportedFail</c>); a case carrying an <c>in.json</c> expectation is supported-valid
/// (<c>SupportedPass</c>). The only cases that cannot be derived from structure are the valid upstream cases the Bodu
/// profile deliberately rejects (<c>UnsupportedFeatureRejected</c>) or parses without a value comparison
/// (<c>SupportedParseOnly</c>); their identifiers are the source of truth held in the two sets below.
/// </para>
/// </remarks>
internal static class YamlTestCorpusReader
{
    /// <summary>
    /// The root directory of the yaml-test-suite cases (the submodule) within the test output.
    /// </summary>
    internal static readonly string CorpusRoot = Path.Combine(AppContext.BaseDirectory, "yaml-test-suite");

    /// <summary>
    /// The total number of vendored cases at the pinned submodule commit.
    /// </summary>
    internal const int ExpectedCaseCount = 402;

    /// <summary>
    /// The expected number of cases in each classification category at the pinned submodule commit. Pinned in code so a
    /// corpus or classification change that shifts the balance is caught by governance.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, int> ExpectedCategoryCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["SupportedPass"] = 229,
            ["SupportedParseOnly"] = 13,
            ["SupportedFail"] = 94,
            ["UnsupportedFeatureRejected"] = 66,
        };

    /// <summary>
    /// The identifiers of valid upstream cases the Bodu profile deliberately rejects. These cannot be derived from the
    /// case structure (the suite publishes them as valid, often with an <c>in.json</c>), so the profile decision is
    /// recorded here as the source of truth.
    /// </summary>
    private static readonly HashSet<string> UnsupportedFeatureRejectedIds = new(StringComparer.Ordinal)
    {
        "26DV",
        "2JQS",
        "2SXE",
        "2XXW",
        "3GZX",
        "3RLN/01",
        "3RLN/04",
        "4FJ6",
        "4QFQ",
        "565N",
        "57H4",
        "5TYM",
        "5WE3",
        "6CA3",
        "6CK3",
        "6PBE",
        "6WLZ",
        "735Y",
        "7FWL",
        "7W2P",
        "8KB6",
        "8UDB",
        "9MMW",
        "9WXW",
        "A2M4",
        "AB8U",
        "BEC7",
        "C4HZ",
        "CC74",
        "CT4Q",
        "CUP7",
        "D83L",
        "DE56/02",
        "DE56/03",
        "DK95/00",
        "E76Z",
        "HMQ5",
        "J7PZ",
        "JTV5",
        "KH5V/01",
        "KK5P",
        "LX3P",
        "M2N8/00",
        "M2N8/01",
        "M5C3",
        "M5DY",
        "M6YH",
        "NJ66",
        "P2AD",
        "P76L",
        "PW8X",
        "Q5MG",
        "Q9WF",
        "R4YG",
        "RZP5",
        "SBG9",
        "SKE5",
        "UGM3",
        "UT92",
        "V9D5",
        "W42U",
        "X38W",
        "XW4D",
        "Z67P",
        "Z9M4",
        "ZWK4",
    };

    /// <summary>
    /// The identifiers of valid upstream cases the profile parses but cannot value-compare (the suite publishes no
    /// <c>in.json</c> for them).
    /// </summary>
    private static readonly HashSet<string> SupportedParseOnlyIds = new(StringComparer.Ordinal)
    {
        "4ABK",
        "6BFJ",
        "6M2F",
        "CFD4",
        "DFF7",
        "FH7J",
        "FRK4",
        "NHX8",
        "NKF9",
        "S3PD",
        "SM9W/01",
        "UKK6/00",
        "UKK6/02",
    };

    /// <summary>
    /// Gets the identifiers the profile classifies by name rather than by structure, for governance against the corpus.
    /// </summary>
    /// <value>The union of the profile-rejected and parse-only identifiers.</value>
    internal static IEnumerable<string> ProfileClassifiedIds =>
        UnsupportedFeatureRejectedIds.Concat(SupportedParseOnlyIds);

    /// <summary>
    /// Returns the absolute directory of a case within the corpus.
    /// </summary>
    /// <param name="id">The case identifier (its path relative to the corpus root).</param>
    /// <returns>The case directory path.</returns>
    internal static string VectorDirectory(string id) =>
        Path.Combine(CorpusRoot, id.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Gets a value indicating whether the corpus submodule is present in the test output. It is absent when the
    /// <c>yaml-test-suite</c> submodule has not been initialized (<c>git submodule update --init</c>).
    /// </summary>
    /// <value><see langword="true" /> when the corpus directory exists; otherwise <see langword="false" />.</value>
    internal static bool IsAvailable => Directory.Exists(CorpusRoot);

    /// <summary>
    /// Enumerates every case identifier in the corpus (a directory containing an <c>in.yaml</c>), ordered. Returns an
    /// empty sequence when the corpus submodule is not present, so callers degrade gracefully rather than throwing.
    /// </summary>
    /// <returns>The case identifiers, ordinal-ordered.</returns>
    internal static IEnumerable<string> EnumerateCaseIds() =>
        IsAvailable
            ? Directory.EnumerateFiles(CorpusRoot, "in.yaml", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(CorpusRoot, Path.GetDirectoryName(path)!).Replace('\\', '/'))
                .OrderBy(id => id, StringComparer.Ordinal)
            : Enumerable.Empty<string>();

    /// <summary>
    /// Enumerates every corpus case with its derived suite kind and profile classification.
    /// </summary>
    /// <returns>One tuple per case: identifier, suite kind, and classification category.</returns>
    internal static IEnumerable<(string Id, string Kind, string Category)> EnumerateCases()
    {
        foreach (string id in EnumerateCaseIds())
        {
            string dir = VectorDirectory(id);
            bool hasError = File.Exists(Path.Combine(dir, "error"));
            bool hasJson = File.Exists(Path.Combine(dir, "in.json"));

            yield return (id, KindOf(hasError, hasJson), Classify(id, hasError, hasJson));
        }
    }

    /// <summary>
    /// Loads every case in the requested classification category into a <see cref="YamlTestVector" />.
    /// </summary>
    /// <param name="category">The classification category to select.</param>
    /// <returns>One vector per case in the category.</returns>
    internal static IEnumerable<YamlTestVector> LoadByCategory(string category)
    {
        foreach ((string? id, string? kind, string? cat) in EnumerateCases())
        {
            if (string.Equals(cat, category, StringComparison.Ordinal))
                yield return Load(id, kind, cat);
        }
    }

    /// <summary>
    /// Derives the suite kind of a case from the expectation files present.
    /// </summary>
    /// <param name="hasError">Whether the case carries an upstream <c>error</c> file.</param>
    /// <param name="hasJson">Whether the case carries an <c>in.json</c> expectation.</param>
    /// <returns>The suite kind: <c>invalid</c>, <c>valid</c>, or <c>valid-nojson</c>.</returns>
    private static string KindOf(bool hasError, bool hasJson) =>
        hasError ? "invalid" : hasJson ? "valid" : "valid-nojson";

    /// <summary>
    /// Classifies a case under the Bodu profile: the by-name profile sets take precedence, then the case structure
    /// (<c>error</c> file then <c>in.json</c>) decides.
    /// </summary>
    /// <param name="id">The case identifier.</param>
    /// <param name="hasError">Whether the case carries an upstream <c>error</c> file.</param>
    /// <param name="hasJson">Whether the case carries an <c>in.json</c> expectation.</param>
    /// <returns>The classification category, or <c>Unknown</c> when the case cannot be attributed.</returns>
    private static string Classify(string id, bool hasError, bool hasJson)
    {
        if (UnsupportedFeatureRejectedIds.Contains(id))
            return "UnsupportedFeatureRejected";

        if (hasError)
            return "SupportedFail";

        if (SupportedParseOnlyIds.Contains(id))
            return "SupportedParseOnly";

        if (hasJson)
            return "SupportedPass";

        return "Unknown";
    }

    /// <summary>
    /// Loads a single case by identifier, reading its description, input, and expectation files.
    /// </summary>
    /// <param name="id">The case identifier (its path relative to the corpus root).</param>
    /// <param name="kind">The derived suite kind.</param>
    /// <param name="category">The profile classification category.</param>
    /// <returns>The loaded vector.</returns>
    internal static YamlTestVector Load(string id, string kind, string category)
    {
        string dir = VectorDirectory(id);

        string descriptionPath = Path.Combine(dir, "===");
        string description = File.Exists(descriptionPath) ? File.ReadAllText(descriptionPath).Trim() : string.Empty;

        string jsonPath = Path.Combine(dir, "in.json");
        byte[]? json = File.Exists(jsonPath) ? File.ReadAllBytes(jsonPath) : null;

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
