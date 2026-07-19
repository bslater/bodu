// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlAliasExpansionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the YAML reader rejects an alias-expansion ("billion laughs") amplification bomb with a catchable
/// <see cref="YamlFormatException" /> across every public entry point, and that the serializer clamps a configured
/// maximum depth to the absolute ceiling.
/// </summary>
[TestClass]
public class YamlAliasExpansionTests
{
    /// <summary>
    /// Verifies that <see cref="YamlDocument.Parse(string)" /> rejects an alias bomb rather than materializing an
    /// exponential node tree.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenAliasBomb_ForDocument_ShouldThrowYamlFormatException()
    {
        string bomb = BuildAliasBomb(levels: 8, fanOut: 10);

        Assert.ThrowsExactly<YamlFormatException>(() => _ = YamlDocument.Parse(bomb));
    }

    /// <summary>
    /// Verifies that <see cref="YamlNode.Parse(string)" /> rejects an alias bomb rather than materializing an
    /// exponential node tree.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenAliasBomb_ForNode_ShouldThrowYamlFormatException()
    {
        string bomb = BuildAliasBomb(levels: 8, fanOut: 10);

        Assert.ThrowsExactly<YamlFormatException>(() => _ = YamlNode.Parse(bomb));
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.Deserialize{T}(string, YamlSerializerOptions)" /> rejects an alias
    /// bomb rather than binding an exponential object graph.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Deserialize_WhenAliasBomb_ForSerializer_ShouldThrowYamlFormatException()
    {
        string bomb = BuildAliasBomb(levels: 8, fanOut: 10);

        Assert.ThrowsExactly<YamlFormatException>(() => _ = YamlSerializer.Deserialize<object>(bomb));
    }

    /// <summary>
    /// Verifies that a serializer <see cref="YamlSerializerOptions.MaxDepth" /> larger than the absolute ceiling is
    /// clamped to <see cref="YamlLimits.AbsoluteMaxDepth" /> — matching the reader and writer option types — so a
    /// large configured depth cannot defeat the recursion guard that protects against a stack overflow.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void EffectiveMaxDepth_WhenConfiguredDepthExceedsAbsoluteCeiling_ShouldClamp()
    {
        var options = new YamlSerializerOptions { MaxDepth = 1000 };

        Assert.AreEqual(YamlLimits.AbsoluteMaxDepth, options.EffectiveMaxDepth);
    }

    /// <summary>
    /// Verifies that a long linear alias chain — whose resolved depth far exceeds the physical nesting clamp — is
    /// rejected with a catchable <see cref="YamlFormatException" /> from the expansion budget rather than crashing
    /// the process with a <see cref="StackOverflowException" /> inside cycle detection.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDeepAliasChainExceedsBudget_ShouldThrowYamlFormatException()
    {
        string chain = BuildAliasChain(length: 100_000);

        Assert.ThrowsExactly<YamlFormatException>(() => _ = YamlDocument.Parse(chain));
    }

    /// <summary>
    /// Verifies that a deep — but within-budget — linear alias chain parses successfully, so the explicit-stack cycle
    /// and budget walks preserve the recursive traversal's accepting behaviour at depths native recursion also
    /// handles.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDeepAliasChainWithinBudget_ShouldParse()
    {
        const int Length = 2000;
        string chain = BuildAliasChain(Length);

        using var document = YamlDocument.Parse(chain);

        Assert.AreEqual(Length + 1, CountMappingEntries(document.RootElement));
    }

    /// <summary>
    /// Verifies that a chain of mappings, each merging the previous via <c>&lt;&lt;</c>, expands transitively so the
    /// final mapping observes every earlier key, and that the injected alias rows stay within the expansion budget.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenChainedMerges_ShouldExpandTransitively()
    {
        const int Links = 300;
        var sb = new StringBuilder();

        sb.Append("m0: &m0\n  k0: 0\n");
        for (int i = 1; i <= Links; i++)
            sb.Append('m').Append(i).Append(": &m").Append(i).Append("\n  <<: *m").Append(i - 1).Append("\n  k").Append(i).Append(": ").Append(i).Append('\n');

        using var document = YamlDocument.Parse(sb.ToString());
        YamlElement last = document.RootElement.GetProperty("m" + Links);

        Assert.AreEqual(Links + 1, CountMappingEntries(last));
        Assert.AreEqual(0L, last.GetProperty("k0").GetInt64());
        Assert.AreEqual((long)Links, last.GetProperty("k" + Links).GetInt64());
    }

    /// <summary>
    /// Counts the entries of a mapping element by enumeration.
    /// </summary>
    /// <param name="element">The mapping element.</param>
    /// <returns>The number of key/value entries.</returns>
    private static int CountMappingEntries(YamlElement element)
    {
        int count = 0;
        foreach (var _ in element.EnumerateMapping())
            count++;

        return count;
    }

    /// <summary>
    /// Builds a document of chained anchors — <c>k1: &amp;a1 [*a0]</c>, <c>k2: &amp;a2 [*a1]</c>, … — whose resolved
    /// alias depth equals the chain length while its physical nesting stays constant.
    /// </summary>
    /// <param name="length">The number of chained links.</param>
    /// <returns>The YAML document text.</returns>
    private static string BuildAliasChain(int length)
    {
        var sb = new StringBuilder();

        sb.Append("k0: &a0 [0]\n");
        for (int i = 1; i <= length; i++)
            sb.Append('k').Append(i).Append(": &a").Append(i).Append(" [*a").Append(i - 1).Append("]\n");

        return sb.ToString();
    }

    /// <summary>
    /// Builds a YAML alias-amplification document: level 0 is a sequence of <paramref name="fanOut" /> scalars, and
    /// each subsequent level is a sequence of <paramref name="fanOut" /> aliases to the level below, so the top level
    /// expands to <paramref name="fanOut" /> raised to <paramref name="levels" /> nodes.
    /// </summary>
    /// <param name="levels">The number of amplification levels.</param>
    /// <param name="fanOut">The number of references at each level.</param>
    /// <returns>The YAML document text.</returns>
    private static string BuildAliasBomb(int levels, int fanOut)
    {
        var sb = new StringBuilder();

        sb.Append("l0: &l0 [");
        for (int i = 0; i < fanOut; i++)
            sb.Append(i == 0 ? "0" : ",0");
        sb.Append("]\n");

        for (int level = 1; level <= levels; level++)
        {
            sb.Append('l').Append(level).Append(": &l").Append(level).Append(" [");
            for (int i = 0; i < fanOut; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append("*l").Append(level - 1);
            }

            sb.Append("]\n");
        }

        return sb.ToString();
    }
}
