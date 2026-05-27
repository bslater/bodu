// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationDotFileExclusionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Pins the PhysicalFileProvider dot-file exclusion contract documented in
/// <see cref="TextConfigurationExtensions.AddConfiguration(IConfigurationBuilder, bool, bool)" />:
/// <see cref="PhysicalFileProvider" /> filters dot-prefixed files via <see cref="ExclusionFilters.Sensitive" />
/// by default, which makes <c>.boduconfig</c> invisible. Callers who want the dot-prefixed convention
/// discovery to work must construct the file provider with <see cref="ExclusionFilters.None" />.
/// </summary>
[TestClass]
public class TextConfigurationDotFileExclusionTests
{
    private const string Sample = """
discovered = yes
""";

    /// <summary>
    /// Verifies that a <see cref="PhysicalFileProvider" /> using the default
    /// <see cref="ExclusionFilters.Sensitive" /> filter does <em>not</em> see a <c>.boduconfig</c> file,
    /// causing the convention-discovery overload to fall back to <c>bodu.config</c> (or, if neither
    /// resolves, miss the dot-file silently when optional).
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenDotFileExistsAndDefaultExclusionFilters_ShouldNotResolveDotFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, ".boduconfig"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory)); // default = ExclusionFilters.Sensitive

            // No bodu.config fallback present, so optional+default-filter discovery sees neither file.
            IConfiguration configuration = builder.AddConfiguration(optional: true).Build();

            Assert.IsNull(configuration["discovered"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the same fixture loads correctly when the <see cref="PhysicalFileProvider" /> is
    /// constructed with <see cref="ExclusionFilters.None" /> — this is the documented workaround in the
    /// extension's XML remarks.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenDotFileExistsAndExclusionFiltersNone_ShouldResolveDotFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, ".boduconfig"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory, ExclusionFilters.None));

            IConfiguration configuration = builder.AddConfiguration().Build();

            Assert.AreEqual("yes", configuration["discovered"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that even with default exclusion filters, the plain <c>bodu.config</c> fallback is still
    /// resolved — only the dot-prefixed name is affected by the default filter.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenPlainFileExistsAndDefaultExclusionFilters_ShouldResolvePlainFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "bodu.config"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory)); // default = Sensitive

            IConfiguration configuration = builder.AddConfiguration().Build();

            Assert.AreEqual("yes", configuration["discovered"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        return directory;
    }
}
