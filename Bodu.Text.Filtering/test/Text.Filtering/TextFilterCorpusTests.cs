// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <summary>
/// Runs the declarative filtering-scenario corpus embedded under <c>Text.Filtering/Fixtures/*.corpus</c>. Each file
/// holds scenarios in a simple line format — <c>scenario:</c> starts a scenario, <c>mode:</c> / <c>ignore-case:</c>
/// set options, <c>include:</c> / <c>exclude:</c> / <c>include-regex:</c> / <c>exclude-regex:</c> add patterns, and
/// <c>match:</c> / <c>skip:</c> state per-value expectations — so the corpus doubles as a language-neutral
/// description of the engine's semantics.
/// </summary>
[TestClass]
public sealed class TextFilterCorpusTests
{
    /// <summary>
    /// One expectation of a corpus scenario: a value and whether the filter must accept it.
    /// </summary>
    /// <param name="Value">The value to evaluate.</param>
    /// <param name="Expected">Whether the value must be accepted.</param>
    public sealed record CorpusExpectation(string Value, bool Expected);

    /// <summary>
    /// One parsed corpus scenario: its source file and name, the filter configuration, and the expectations.
    /// </summary>
    /// <param name="File">The corpus file the scenario came from.</param>
    /// <param name="Title">The scenario title.</param>
    /// <param name="Mode">The evaluation mode.</param>
    /// <param name="IgnoreCase">The case-insensitivity default.</param>
    /// <param name="Patterns">The declared patterns in declaration order.</param>
    /// <param name="Expectations">The per-value expectations.</param>
    public sealed record FilterScenarioKat(
        string File,
        string Title,
        TextFilterEvaluationMode Mode,
        bool IgnoreCase,
        IReadOnlyList<TextFilterPattern> Patterns,
        IReadOnlyList<CorpusExpectation> Expectations) : IKat
    {
        /// <inheritdoc />
        public string Name =>
            $"{File}: {Title}";
    }

    /// <summary>
    /// Gets every scenario parsed from the embedded corpus files.
    /// </summary>
    public static IEnumerable<object[]> Scenarios =>
        LoadScenarios().Select(static s => new object[] { s });

    /// <summary>
    /// Verifies every value expectation of every corpus scenario against a filter built from the scenario's patterns
    /// and options.
    /// </summary>
    /// <param name="kat">The corpus scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(Scenarios), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void IsMatch_WhenCorpusScenarioEvaluated_ShouldSatisfyEveryExpectation(FilterScenarioKat kat)
    {
        var filter = TextFilter.Build(kat.Patterns, new TextFilterOptions { Mode = kat.Mode, IgnoreCase = kat.IgnoreCase });

        foreach (var expectation in kat.Expectations)
        {
            if (filter.IsMatch(expectation.Value) != expectation.Expected)
                Assert.Fail($"'{expectation.Value}' expected {(expectation.Expected ? "match" : "skip")} in scenario '{kat.Name}'.");
        }
    }

    /// <summary>
    /// Verifies that the corpus is wired and non-trivial — a guard against silent resource-embedding breakage that
    /// would otherwise make the Regression sweep vacuously pass.
    /// </summary>
    [TestMethod]
    public void Scenarios_WhenCorpusLoaded_ShouldContainFilesScenariosAndExpectations()
    {
        var scenarios = LoadScenarios();

        Assert.IsTrue(scenarios.Select(static s => s.File).Distinct().Count() >= 6, "Expected at least six corpus files.");
        Assert.IsTrue(scenarios.Count >= 25, $"Expected at least 25 scenarios, found {scenarios.Count}.");
        Assert.IsTrue(scenarios.Sum(static s => s.Expectations.Count) >= 90, "Expected at least 90 value expectations.");
        Assert.IsTrue(scenarios.All(static s => s.Expectations.Count > 0), "Every scenario must state at least one expectation.");
    }

    /// <summary>
    /// Loads and parses every embedded corpus file.
    /// </summary>
    /// <returns>The parsed scenarios, in file order then declaration order.</returns>
    private static List<FilterScenarioKat> LoadScenarios()
    {
        var assembly = typeof(TextFilterCorpusTests).Assembly;
        var scenarios = new List<FilterScenarioKat>();

        foreach (var resource in assembly.GetManifestResourceNames().Where(static n => n.EndsWith(".corpus", StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var shortName = resource.Split('.')[^2];

            ParseFile(shortName, reader.ReadToEnd(), scenarios);
        }

        return scenarios;
    }

    /// <summary>
    /// Parses one corpus file's text into scenarios.
    /// </summary>
    /// <param name="file">The short corpus-file name used in scenario display names.</param>
    /// <param name="text">The file content.</param>
    /// <param name="scenarios">The accumulator receiving parsed scenarios.</param>
    private static void ParseFile(string file, string text, List<FilterScenarioKat> scenarios)
    {
        string? title = null;
        var mode = TextFilterEvaluationMode.AnyMatch;
        var ignoreCase = true;
        var patterns = new List<TextFilterPattern>();
        var expectations = new List<CorpusExpectation>();

        void Flush()
        {
            if (title is not null)
                scenarios.Add(new FilterScenarioKat(file, title, mode, ignoreCase, [.. patterns], [.. expectations]));

            mode = TextFilterEvaluationMode.AnyMatch;
            ignoreCase = true;
            patterns = [];
            expectations = [];
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            Assert.IsTrue(separator > 0, $"Malformed corpus line in '{file}': {line}");

            var keyword = line[..separator];
            var argument = separator + 2 <= line.Length ? line[(separator + 2)..] : string.Empty;
            switch (keyword)
            {
                case "scenario":
                    Flush();
                    title = argument;
                    break;

                case "mode":
                    mode = Enum.Parse<TextFilterEvaluationMode>(argument);
                    break;

                case "ignore-case":
                    ignoreCase = bool.Parse(argument);
                    break;

                case "include":
                    patterns.Add(TextFilterPattern.Include(argument));
                    break;

                case "exclude":
                    patterns.Add(TextFilterPattern.Exclude(argument));
                    break;

                case "include-regex":
                    patterns.Add(TextFilterPattern.Include(argument, TextFilterPatternKind.Regex));
                    break;

                case "exclude-regex":
                    patterns.Add(TextFilterPattern.Exclude(argument, TextFilterPatternKind.Regex));
                    break;

                case "match":
                    expectations.Add(new CorpusExpectation(argument, true));
                    break;

                case "skip":
                    expectations.Add(new CorpusExpectation(argument, false));
                    break;

                default:
                    Assert.Fail($"Unknown corpus keyword '{keyword}' in '{file}'.");
                    break;
            }
        }

        Flush();
    }
}
