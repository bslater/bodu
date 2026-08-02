// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Stress-tier scale runs — high-volume, large-input, and high-concurrency loops that exceed the standard session
/// guard budget of the default tiers. Run on demand via <c>stress.runsettings</c>.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies a ten-million-value streamed sweep through a mixed-tier filter: every sampled value agrees with the
    /// exhaustive all-patterns evaluation, and the statistics buckets reconcile exactly with the volume.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void IsMatch_WhenTenMillionValuesStreamed_ShouldReconcileStatisticsAndAgreeWithSampledOracle()
    {
        const int Volume = 10_000_000;
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("error*"),
            TextFilterPattern.Include("{warn,fatal}*"),
            TextFilterPattern.Include("*-critical"),
            TextFilterPattern.Include(@"^audit-\d+\.log$", TextFilterPatternKind.Regex),
            TextFilterPattern.Exclude("*.tmp"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        var random = new Random(8_675_309);
        string[] stems = ["error", "warn", "info", "debug", "trace", "fatal", "audit", "metric"];
        var accepted = 0L;
        for (var i = 0; i < Volume; i++)
        {
            // Values are generated one at a time so the sweep stays memory-flat at this volume.
            var stem = stems[random.Next(stems.Length)];
            var value = $"{stem}-{random.Next(10_000)}{(random.Next(5) == 0 ? ".tmp" : ".log")}";

            var isMatch = filter.IsMatch(value);
            if (isMatch)
                accepted++;

            // A prime stride keeps the oracle sampling cheap while touching every stem/suffix combination.
            if (i % 997 == 0 && isMatch != ExpectedAnyMatch(filter, value))
                Assert.Fail($"Disagreement with the exhaustive oracle on '{value}' at index {i}.");
        }

        var stats = filter.GetStatistics();
        Assert.AreEqual(Volume, stats.ItemsEvaluated);
        Assert.AreEqual(accepted, stats.ItemsAccepted);
        Assert.AreEqual(stats.ItemsEvaluated, stats.ItemsAccepted + stats.ItemsExcluded + stats.ItemsNotIncluded);
        Assert.AreEqual(0, stats.RegexTimeouts);
    }

    /// <summary>
    /// Verifies that a filter compiled from ten thousand patterns spanning every cost tier evaluates a 50k-value
    /// corpus in agreement with the exhaustive all-patterns evaluation on sampled values.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Build_WhenTenThousandPatterns_ShouldCompileAndEvaluateInAgreementWithOracle()
    {
        var patterns = new List<TextFilterPattern>(10_000);
        for (var i = 0; i < 10_000; i++)
        {
            patterns.Add((i % 8) switch
            {
                0 => TextFilterPattern.Include($"literal-{i:d5}.log"),
                1 => TextFilterPattern.Include($"prefix-{i % 100:d2}*"),
                2 => TextFilterPattern.Include($"*-suffix-{i % 100:d2}"),
                3 => TextFilterPattern.Include($"*core-{i % 100:d2}*"),
                4 => TextFilterPattern.Include($"{{alpha,beta}}-{i % 100:d2}*"),
                5 => TextFilterPattern.Include($"class-[0-9]-{i % 100:d2}*"),
                6 when i % 40 == 6 => TextFilterPattern.Include($@"^regex-{i % 100:d2}-\d+$", TextFilterPatternKind.Regex),
                6 => TextFilterPattern.Include($"general-?-{i % 100:d2}*"),
                _ => TextFilterPattern.Exclude($"*banned-{i % 100:d2}*"),
            });
        }

        var filter = TextFilter.Build(patterns);
        Assert.AreEqual(10_000, filter.Patterns.Count);

        var random = new Random(24_601);
        string[] shapes =
        [
            "literal-{0:d5}.log", "prefix-{1:d2}-x", "y-suffix-{1:d2}", "has-core-{1:d2}-inside",
            "alpha-{1:d2}-z", "beta-{1:d2}-z", "class-7-{1:d2}-q", "general-x-{1:d2}-r",
            "regex-{1:d2}-123", "plain-{0:d5}", "x-banned-{1:d2}-y", "prefix-{1:d2}-banned-{1:d2}",
        ];
        for (var i = 0; i < 50_000; i++)
        {
            var value = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                shapes[random.Next(shapes.Length)],
                random.Next(10_000),
                random.Next(100));

            var isMatch = filter.IsMatch(value);
            if (i % 173 == 0 && isMatch != ExpectedAnyMatch(filter, value))
                Assert.Fail($"Disagreement with the exhaustive oracle on '{value}' at index {i}.");
        }

        var stats = filter.GetStatistics();
        Assert.AreEqual(50_000, stats.ItemsEvaluated);
        Assert.AreEqual(stats.ItemsEvaluated, stats.ItemsAccepted + stats.ItemsExcluded + stats.ItemsNotIncluded);
    }

    /// <summary>
    /// Verifies that the general matcher's star-backtracking worst case — hundreds of <c>*a</c> segments against
    /// long all-<c>a</c> values — stays polynomial and correct across repeated large matches.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void IsMatch_WhenPathologicalStarPatternsAtScale_ShouldStayPolynomialAndCorrect()
    {
        var pattern = string.Concat(Enumerable.Repeat("*a", 200)) + "*b";
        var filter = TextFilter.Build([TextFilterPattern.Include(pattern, ignoreCase: false)]);
        var almost = new string('a', 20_000);
        var full = almost + "b";

        // A naive exponential matcher never finishes one of these; the two-pointer matcher does ~n*m unit steps.
        for (var i = 0; i < 25; i++)
        {
            Assert.IsFalse(filter.IsMatch(almost));
            Assert.IsTrue(filter.IsMatch(full));
        }
    }

    /// <summary>
    /// Verifies every wildcard strategy against megabyte-scale values — the literal, prefix, suffix, contains, and
    /// general tiers must all stay correct when a single value is a million characters long.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void IsMatch_WhenValuesAreMegabyteScale_ShouldMatchCorrectlyAcrossStrategies()
    {
        var megabyte = new string('x', 1_000_000);
        var withNeedle = string.Concat(megabyte.AsSpan(0, 500_000), "needle", megabyte.AsSpan(0, 500_000));

        // Patterns are capped at 4,096 characters by design; the literal tier is stressed at that cap while the
        // values themselves are a million characters long.
        var capLiteral = new string('L', 4_088) + "-literal";
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include(capLiteral),
            TextFilterPattern.Include("head*"),
            TextFilterPattern.Include("*.tail"),
            TextFilterPattern.Include("*needle*"),
            TextFilterPattern.Include("a*[0-9]z"),
        ]);

        for (var i = 0; i < 50; i++)
        {
            Assert.IsTrue(filter.IsMatch(capLiteral));
            Assert.IsTrue(filter.IsMatch("head" + megabyte));
            Assert.IsTrue(filter.IsMatch(megabyte + ".tail"));
            Assert.IsTrue(filter.IsMatch(withNeedle));
            Assert.IsTrue(filter.IsMatch("a" + megabyte + "5z"));
            Assert.IsFalse(filter.IsMatch(megabyte));
        }
    }

    /// <summary>
    /// Verifies that concurrent evaluation on one shared filter stays exact for every thread — the compiled matching
    /// state is immutable, so eight threads hammering a million evaluations each must reproduce the single-threaded
    /// outcomes with zero disagreements (only the statistics counters are allowed to undercount, per contract).
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void IsMatch_WhenHammeredConcurrently_ShouldStayExactOnEveryThread()
    {
        const int Threads = 8;
        const int PerThread = 1_000_000;
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("{error,warn}*"),
            TextFilterPattern.Include("job-[0-9][0-9]"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        string[] values = ["error-1", "warn-2", "error-debug-3", "info-4", "job-42", "job-7x", "WARN-5", "warn-debug-6"];
        var expected = values.Select(filter.IsMatch).ToArray();

        var disagreements = 0L;
        Parallel.For(0, Threads, _ =>
        {
            var local = 0L;
            for (var i = 0; i < PerThread; i++)
            {
                var slot = i & 7;
                if (filter.IsMatch(values[slot]) != expected[slot])
                    local++;
            }

            Interlocked.Add(ref disagreements, local);
        });

        Assert.AreEqual(0, disagreements);

        // Counters may undercount under concurrency but must never exceed the work performed.
        var evaluated = filter.GetStatistics().ItemsEvaluated;
        Assert.IsTrue(evaluated > 0 && evaluated <= (Threads * (long)PerThread) + values.Length);
    }

    /// <summary>
    /// Verifies that repeatedly building filters whose brace alternation expands near the per-pattern cap stays
    /// bounded and correct — 50 builds of a pattern expanding into 8,192 literal alternatives.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Build_WhenBraceExpansionNearCapRepeatedly_ShouldStayBoundedAndCorrect()
    {
        var pattern = string.Concat(Enumerable.Repeat("{a,b}", 13));

        for (var i = 0; i < 50; i++)
        {
            var filter = TextFilter.Build([TextFilterPattern.Include(pattern)]);

            Assert.AreEqual(1, filter.Patterns.Count);
            Assert.IsTrue(filter.IsMatch("abababababab" + (i % 2 == 0 ? "a" : "b")));
            Assert.IsFalse(filter.IsMatch("abababababab"));
            Assert.IsFalse(filter.IsMatch("ababababababac"));
        }
    }
}
