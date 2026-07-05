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
            sb.Append("l").Append(level).Append(": &l").Append(level).Append(" [");
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
