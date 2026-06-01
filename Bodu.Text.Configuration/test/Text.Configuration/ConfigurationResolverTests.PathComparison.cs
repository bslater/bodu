// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.PathComparison.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Pins that <see cref="ConfigurationResolveOptions.PathComparison" /> actually drives glob matching during
/// resolution. The option existed before but the regex emitted by <see cref="ConfigurationPattern" /> was
/// always built without case folding, which made the option a no-op for case differences.
/// </summary>
public partial class ConfigurationResolverTests
{
    /// <summary>
    /// Verifies that under the default ordinal comparison a pattern whose case differs from the target path
    /// does not match.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathComparisonIsOrdinal_ShouldNotMatchPatternWithDifferentCase()
    {
        const string fixture = """
[*.CS]
format.indent.size = 2
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathComparison = StringComparison.Ordinal };
        ConfigurationView view = doc.Resolve("foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that under <see cref="StringComparison.OrdinalIgnoreCase" /> a pattern whose case differs
    /// from the target path matches.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathComparisonIsOrdinalIgnoreCase_ShouldMatchPatternRegardlessOfCase()
    {
        const string fixture = """
[*.CS]
format.indent.size = 2
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathComparison = StringComparison.OrdinalIgnoreCase };
        ConfigurationView view = doc.Resolve("foo.cs", options);

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that the inverse case (pattern lowercase, target uppercase) also matches under
    /// case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathComparisonIsOrdinalIgnoreCase_ShouldMatchTargetRegardlessOfCase()
    {
        const string fixture = """
[*.cs]
format.indent.size = 2
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathComparison = StringComparison.OrdinalIgnoreCase };
        ConfigurationView view = doc.Resolve("FOO.CS", options);

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that case-insensitive matching applies to character classes as well as literal segments.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathComparisonIsOrdinalIgnoreCase_ShouldMatchCharacterClassRegardlessOfCase()
    {
        const string fixture = """
[file-[A-Z].txt]
hit = yes
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathComparison = StringComparison.OrdinalIgnoreCase };
        ConfigurationView view = doc.Resolve("file-a.txt", options);

        Assert.AreEqual("yes", view.GetString("hit"));
    }
}
