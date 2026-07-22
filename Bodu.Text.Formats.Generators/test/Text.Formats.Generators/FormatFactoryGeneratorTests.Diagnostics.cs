// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Contains the driver-based diagnostic tests for <see cref="FormatFactoryGenerator" />, compiling annotated
/// snippets in-memory and asserting the reported <c>BTFG</c> diagnostics.
/// </summary>
public partial class FormatFactoryGeneratorTests
{
    /// <summary>
    /// Verifies that an annotated type that is not partial reports <c>BTFG001</c> and generates no source.
    /// </summary>
    [TestMethod]
    public void Generator_WhenTypeNotPartial_ShouldReportBtfg001()
    {
        const string Source = """
            using Bodu.Text.Delimited;

            namespace Sample;

            [DelimitedRecord]
            public sealed class NotPartial
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(Source);

        Assert.IsTrue(result.Diagnostics.Any(d => d.Id == "BTFG001"), "expected BTFG001");
        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    /// <summary>
    /// Verifies that an annotated generic type reports <c>BTFG003</c> and generates no source.
    /// </summary>
    [TestMethod]
    public void Generator_WhenTypeGeneric_ShouldReportBtfg003()
    {
        const string Source = """
            using Bodu.Text.Ini;

            namespace Sample;

            [IniSection]
            public sealed partial class Generic<T>
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(Source);

        Assert.IsTrue(result.Diagnostics.Any(d => d.Id == "BTFG003"), "expected BTFG003");
        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    /// <summary>
    /// Verifies that an unsupported member type reports <c>BTFG002</c> while the factory is still generated for the
    /// remaining members.
    /// </summary>
    [TestMethod]
    public void Generator_WhenMemberTypeUnsupported_ShouldReportBtfg002AndSkipMember()
    {
        const string Source = """
            using System.Collections.Generic;
            using Bodu.Text.Delimited;

            namespace Sample;

            [DelimitedRecord]
            public sealed partial class HasUnsupported
            {
                public string Name { get; set; } = string.Empty;

                public List<int> Values { get; set; } = new();
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(Source);

        Assert.IsTrue(result.Diagnostics.Any(d => d.Id == "BTFG002"), "expected BTFG002");
        Assert.AreEqual(1, result.GeneratedTrees.Length);

        string generated = result.GeneratedTrees[0].ToString();
        Assert.IsTrue(generated.Contains("\"Name\"", StringComparison.Ordinal));
        Assert.IsFalse(generated.Contains("Values", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a nested annotated type generates a factory wrapped in its partial containing types.
    /// </summary>
    [TestMethod]
    public void Generator_WhenTypeNested_ShouldWrapContainingTypes()
    {
        const string Source = """
            using Bodu.Text.Delimited;

            namespace Sample;

            public partial class Outer
            {
                [DelimitedRecord]
                public sealed partial class Inner
                {
                    public string Name { get; set; } = string.Empty;
                }
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(Source);

        Assert.AreEqual(0, result.Diagnostics.Length);
        Assert.AreEqual(1, result.GeneratedTrees.Length);

        string generated = result.GeneratedTrees[0].ToString();
        Assert.IsTrue(generated.Contains("partial class Outer", StringComparison.Ordinal));
        Assert.IsTrue(generated.Contains("partial class Inner", StringComparison.Ordinal));
    }

    /// <summary>
    /// Runs the generator over the supplied snippet compiled against this test's referenced assemblies.
    /// </summary>
    /// <param name="source">The snippet source.</param>
    /// <returns>The generator run result.</returns>
    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "Bodu.GeneratorTests.Snippets",
            new[] { tree },
            LoadTrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FormatFactoryGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Builds the metadata references from the runtime's trusted platform assemblies, which include this test's
    /// referenced Bodu assemblies.
    /// </summary>
    /// <returns>The metadata references for snippet compilation.</returns>
    private static List<MetadataReference> LoadTrustedPlatformReferences()
    {
        string trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        return trusted
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }
}
