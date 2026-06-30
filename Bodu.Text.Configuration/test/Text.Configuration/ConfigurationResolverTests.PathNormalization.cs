// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.PathNormalization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationResolverTests
{
    /// <summary>
    /// Verifies that backslashes in the target path are normalized to forward slashes before matching.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenTargetPathUsesBackslashes_ShouldNormalizeToForwardSlashesBeforeMatching()
    {
        const string fixture = """
[src/**/*.cs]
format.indent.size = 2
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationView windowsStyle = doc.Resolve(@"src\Foo.cs");
        ConfigurationView unixStyle = doc.Resolve("src/Foo.cs");

        Assert.AreEqual(windowsStyle.GetString("format:indent:size"), unixStyle.GetString("format:indent:size"));
    }

    /// <summary>
    /// Verifies that when <see cref="ConfigurationResolveOptions.PathRoot" /> is set, the resolver
    /// strips the root from the target before matching.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathRootIsSet_ShouldStripRootFromTargetPath()
    {
        const string fixture = """
[Foo.cs]
format.indent.size = 2
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathRoot = "/project" };
        ConfigurationView view = doc.Resolve("/project/Foo.cs", options);

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that, under the default case-sensitive <see cref="ConfigurationResolveOptions.PathComparison" /> (
    /// <see cref="System.StringComparison.Ordinal" />), a path root whose case does not match the target is <b>not</b>
    /// stripped, so an anchored section pattern fails to match. This keeps path-root rebasing consistent with the case
    /// sensitivity used for glob matching.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathRootCaseDiffersUnderOrdinal_ShouldNotStripRoot()
    {
        const string fixture = """
[src/Foo.cs]
format.indent.size = 2
""";
        var doc = ConfigurationDocument.Parse(fixture);

        // Default PathComparison is Ordinal (case-sensitive); the root "/project" does not match "/Project/...".
        ConfigurationResolveOptions options = new() { PathRoot = "/project" };
        ConfigurationView view = doc.Resolve("/Project/src/Foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
    }

    /// <summary>
    /// Verifies that when <see cref="ConfigurationResolveOptions.PathComparison" /> is case-insensitive, a path root
    /// whose case differs from the target is still stripped, so the anchored section pattern matches.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathRootCaseDiffersUnderIgnoreCase_ShouldStripRoot()
    {
        const string fixture = """
[src/Foo.cs]
format.indent.size = 2
""";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new()
        {
            PathRoot = "/project",
            PathComparison = StringComparison.OrdinalIgnoreCase,
        };
        ConfigurationView view = doc.Resolve("/Project/src/Foo.cs", options);

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that under the strict <see cref="ConfigurationMissingPathRootMode.Throw" /> mode,
    /// resolving without a path root throws.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMissingPathRootModeIsThrow_ShouldThrowExactly()
    {
        const string fixture = "application.name = Bodu\n";
        var doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new()
        {
            MissingPathRootMode = ConfigurationMissingPathRootMode.Throw,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = doc.Resolve(targetPath: null, options);
        });
    }
}
