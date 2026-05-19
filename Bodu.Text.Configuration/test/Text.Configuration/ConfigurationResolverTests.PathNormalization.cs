// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverTests.PathNormalization.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

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
        IniDocument doc = ConfigurationDocument.Parse(fixture);

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
        IniDocument doc = ConfigurationDocument.Parse(fixture);

        ConfigurationResolveOptions options = new() { PathRoot = "/project" };
        ConfigurationView view = doc.Resolve("/project/Foo.cs", options);

        Assert.AreEqual(2, view.GetInt32("format:indent:size"));
    }

    /// <summary>
    /// Verifies that under the strict <see cref="ConfigurationMissingPathRootMode.Throw" /> mode,
    /// resolving without a path root throws.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMissingPathRootModeIsThrow_ShouldThrowWhenNoContext()
    {
        const string fixture = "application.name = Bodu\n";
        IniDocument doc = ConfigurationDocument.Parse(fixture);

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
