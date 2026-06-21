// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Bodu.Test;
using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Smoke;

/// <summary>
/// Smoke tests for the Bodu Microsoft.Extensions.Configuration bridge.
/// </summary>
[TestClass]
public class BridgeSmokeTests
{
    /// <summary>
    /// Verifies that <see cref="TextConfigurationExtensions.AddTextConfigurationFile(IConfigurationBuilder, string, string?, bool, bool)" />
    /// loads a configuration file and exposes its keys in colon-delimited form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddConfiguration_ShouldExposeColonDelimitedKeys()
    {
        using TempFileScope scope = new("""
[*.cs]
logging.level.default = Information

[src/**/*.cs]
logging.level.default = Warning
""");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile(source =>
            {
                source.FileProvider = new PhysicalFileProvider(scope.Directory);
                source.Path = Path.GetFileName(scope.Path);
                source.TargetPath = "src/Foo.cs";
            })
            .Build();

        Assert.AreEqual("Warning", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that an optional missing file does not throw and produces an empty configuration view.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenOptionalAndMissing_ShouldNotThrow()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile("nonexistent.boduconfig", optional: true)
            .Build();

        Assert.IsNull(configuration["any:key"]);
    }
}
