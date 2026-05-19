// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Extensions.Configuration.Text;
using Bodu.Test;
using Bodu.Text.Configuration;
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
    /// Verifies that <see cref="BoduTextConfigurationExtensions.AddConfiguration(IConfigurationBuilder, string, string?, bool, bool)" />
    /// loads a configuration file and exposes its keys in colon-delimited form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void AddConfiguration_ShouldExposeColonDelimitedKeys()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
[*.cs]
logging.level.default = Information

[src/**/*.cs]
logging.level.default = Warning
""");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddConfiguration(source =>
                {
                    source.FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(path)!);
                    source.Path = Path.GetFileName(path);
                    source.TargetPath = "src/Foo.cs";
                })
                .Build();

            Assert.AreEqual("Warning", configuration["logging:level:default"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that an optional missing file does not throw and produces an empty configuration view.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenOptionalAndMissing_ShouldNotThrow()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration("nonexistent.boduconfig", optional: true)
            .Build();

        Assert.IsNull(configuration["any:key"]);
    }
}
