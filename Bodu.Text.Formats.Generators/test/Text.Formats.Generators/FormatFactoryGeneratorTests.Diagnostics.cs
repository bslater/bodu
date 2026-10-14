// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Text.Serialization;

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
    /// Verifies that a member whose type is not a supported scalar, which the reflection binder would reject, reports
    /// <c>BTFG002</c> as an error naming the member and the type, and that no factory is emitted for the type, for a
    /// delimited record and an INI section alike.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    /// <param name="memberType">The member's type, as written in source.</param>
    [TestMethod]
    [DataRow("DelimitedRecord", "List<int>")]
    [DataRow("DelimitedRecord", "Dictionary<string, int>")]
    [DataRow("DelimitedRecord", "object")]
    [DataRow("IniSection", "List<int>")]
    [DataRow("IniSection", "Dictionary<string, int>")]
    [DataRow("IniSection", "object")]
    public void Generator_WhenMemberTypeUnsupported_ShouldReportBtfg002AsAnErrorAndEmitNoFactory(string attribute, string memberType)
    {
        string source = $$"""
            using System.Collections.Generic;
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;

            namespace Sample;

            [{{attribute}}]
            public sealed partial class HasUnsupported
            {
                public string Name { get; set; } = string.Empty;

                public {{memberType}} Values { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(source);

        Diagnostic diagnostic = result.Diagnostics.Single(static d => d.Id == "BTFG002");
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Values'", StringComparison.Ordinal), diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    /// <summary>
    /// Verifies that a member whose type is not a supported scalar, left out deliberately with <c>[Ignore]</c>, reports
    /// no diagnostic, and that the factory is emitted for the remaining members and compiles, for a delimited record and
    /// an INI section alike.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    [TestMethod]
    [DataRow("DelimitedRecord")]
    [DataRow("IniSection")]
    public void Generator_WhenAnUnsupportedMemberIsIgnored_ShouldEmitTheFactoryWithoutADiagnostic(string attribute)
    {
        string source = $$"""
            #nullable enable
            using System.Collections.Generic;
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;
            using Bodu.Text.Serialization;

            namespace Sample;

            [{{attribute}}]
            public sealed partial class HasIgnored
            {
                public string Name { get; set; } = string.Empty;

                [Ignore]
                public List<int> Values { get; set; } = new();
            }
            """;

        (GeneratorDriverRunResult result, IReadOnlyList<Diagnostic> errors) = RunGeneratorAndCompile(source);

        Assert.AreEqual(0, result.Diagnostics.Length, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(1, result.GeneratedTrees.Length);
        Assert.IsFalse(result.GeneratedTrees[0].ToString().Contains("Values", StringComparison.Ordinal));
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// Verifies that a public property the factory does not map, because it is get-only, <c>init</c>-only or inherited,
    /// reports <c>BTFG002</c> as an error naming the member when its type is not a supported scalar, since the
    /// reflection binders map the member and reject its type, and that no factory is emitted for the type, for a
    /// delimited record and an INI section alike.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    /// <param name="shape">How the member is declared: <c>get-only</c>, <c>init-only</c> or <c>inherited</c>.</param>
    [TestMethod]
    [DataRow("DelimitedRecord", "get-only")]
    [DataRow("DelimitedRecord", "init-only")]
    [DataRow("DelimitedRecord", "inherited")]
    [DataRow("IniSection", "get-only")]
    [DataRow("IniSection", "init-only")]
    [DataRow("IniSection", "inherited")]
    public void Generator_WhenAnUnmappedMemberTypeIsUnsupported_ShouldReportBtfg002AsAnErrorAndEmitNoFactory(string attribute, string shape)
    {
        string member = shape switch
        {
            "get-only" => "public List<int> Values { get; } = new();",
            "init-only" => "public List<int> Values { get; init; } = new();",
            "inherited" => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };
        string baseClause = shape == "inherited" ? " : RecordBase" : string.Empty;

        string source = $$"""
            using System.Collections.Generic;
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;

            namespace Sample;

            public class RecordBase
            {
                public List<int> Values { get; set; } = new();
            }

            [{{attribute}}]
            public partial class HasUnmapped{{baseClause}}
            {
                public string Name { get; set; } = string.Empty;

                {{member}}
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(source);

        Diagnostic diagnostic = result.Diagnostics.Single(static d => d.Id == "BTFG002");
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.IsTrue(diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("'Values'", StringComparison.Ordinal), diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    /// <summary>
    /// Verifies that an inherited member whose type is not a supported scalar, left out with <c>[Ignore]</c> where the
    /// base type declares it, reports no diagnostic, and that the factory is emitted for the remaining members and
    /// compiles, for a delimited record and an INI section alike.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    [TestMethod]
    [DataRow("DelimitedRecord")]
    [DataRow("IniSection")]
    public void Generator_WhenAnUnsupportedInheritedMemberIsIgnored_ShouldEmitTheFactoryWithoutADiagnostic(string attribute)
    {
        string source = $$"""
            #nullable enable
            using System.Collections.Generic;
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;
            using Bodu.Text.Serialization;

            namespace Sample;

            public class RecordBase
            {
                [Ignore]
                public List<int> Values { get; set; } = new();
            }

            [{{attribute}}]
            public sealed partial class HasIgnoredBase : RecordBase
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        (GeneratorDriverRunResult result, IReadOnlyList<Diagnostic> errors) = RunGeneratorAndCompile(source);

        Assert.AreEqual(0, result.Diagnostics.Length, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(1, result.GeneratedTrees.Length);
        Assert.IsFalse(result.GeneratedTrees[0].ToString().Contains("Values", StringComparison.Ordinal));
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// Verifies that an inherited member whose type is not a supported scalar, hidden with <see langword="new" /> by a
    /// scalar member of the same name, reports no diagnostic, since the reflection binders map only the most derived
    /// declaration of a name, and that the factory maps the scalar and compiles, for a delimited record and an INI
    /// section alike.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    [TestMethod]
    [DataRow("DelimitedRecord")]
    [DataRow("IniSection")]
    public void Generator_WhenAnUnsupportedInheritedMemberIsHiddenByAScalar_ShouldMapTheScalarWithoutADiagnostic(string attribute)
    {
        string source = $$"""
            #nullable enable
            using System.Collections.Generic;
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;

            namespace Sample;

            public class RecordBase
            {
                public List<int> Values { get; set; } = new();
            }

            [{{attribute}}]
            public sealed partial class HidesBase : RecordBase
            {
                public new string Values { get; set; } = string.Empty;
            }
            """;

        (GeneratorDriverRunResult result, IReadOnlyList<Diagnostic> errors) = RunGeneratorAndCompile(source);

        Assert.AreEqual(0, result.Diagnostics.Length, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(1, result.GeneratedTrees.Length);
        Assert.IsTrue(result.GeneratedTrees[0].ToString().Contains("\"Values\"", StringComparison.Ordinal));
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
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

    /// <summary>
    /// Verifies that a member of a scalar type the reflection binders convert, alone or in nullable form, is mapped
    /// rather than refused with <c>BTFG002</c>, and that the factory generated for it compiles.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    /// <param name="memberType">The member's type, as written in source.</param>
    [TestMethod]
    [DataRow("DelimitedRecord", "System.DateOnly")]
    [DataRow("DelimitedRecord", "System.TimeOnly")]
    [DataRow("DelimitedRecord", "System.Uri")]
    [DataRow("DelimitedRecord", "System.Uri?")]
    [DataRow("DelimitedRecord", "System.Int128")]
    [DataRow("DelimitedRecord", "System.UInt128")]
    [DataRow("DelimitedRecord", "System.Half")]
    [DataRow("DelimitedRecord", "System.Numerics.BigInteger")]
    [DataRow("DelimitedRecord", "nint")]
    [DataRow("DelimitedRecord", "nuint")]
    [DataRow("DelimitedRecord", "System.DateOnly?")]
    [DataRow("DelimitedRecord", "System.Int128?")]
    [DataRow("DelimitedRecord", "nint?")]
    [DataRow("IniSection", "System.DateOnly")]
    [DataRow("IniSection", "System.TimeOnly")]
    [DataRow("IniSection", "System.Uri")]
    [DataRow("IniSection", "System.Uri?")]
    [DataRow("IniSection", "System.Int128")]
    [DataRow("IniSection", "System.UInt128")]
    [DataRow("IniSection", "System.Half")]
    [DataRow("IniSection", "System.Numerics.BigInteger")]
    [DataRow("IniSection", "nint")]
    [DataRow("IniSection", "nuint")]
    [DataRow("IniSection", "System.DateOnly?")]
    [DataRow("IniSection", "System.Int128?")]
    [DataRow("IniSection", "nint?")]
    public void Generator_WhenMemberTypeIsAScalarTheReflectionBindersConvert_ShouldMapIt(string attribute, string memberType)
    {
        string source = $$"""
            #nullable enable
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;

            namespace Sample;

            [{{attribute}}]
            public sealed partial class Holder
            {
                public string Name { get; set; } = string.Empty;

                public {{memberType}} Value { get; set; } = default!;
            }
            """;

        (GeneratorDriverRunResult result, IReadOnlyList<Diagnostic> errors) = RunGeneratorAndCompile(source);

        Assert.IsFalse(result.Diagnostics.Any(static d => d.Id == "BTFG002"), string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(1, result.GeneratedTrees.Length);
        Assert.IsTrue(result.GeneratedTrees[0].ToString().Contains("\"Value\"", StringComparison.Ordinal));
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// Runs the generator over the supplied snippet and compiles the snippet with the generated source.
    /// </summary>
    /// <param name="source">The snippet source.</param>
    /// <returns>The generator run result, and the errors of the compilation that includes the generated source.</returns>
    private static (GeneratorDriverRunResult Result, IReadOnlyList<Diagnostic> Errors) RunGeneratorAndCompile(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "Bodu.GeneratorTests.Snippets",
            new[] { tree },
            LoadTrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FormatFactoryGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);

        Diagnostic[] errors = [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
        return (driver.GetRunResult(), errors);
    }

    /// <summary>
    /// Verifies that every scalar type the reflection binders' codec lists, and an enumeration, alone and in nullable
    /// form, is mapped by the generator for both formats rather than refused with <c>BTFG002</c>, so that the
    /// generator's scalar set cannot drift from the binders' again.
    /// </summary>
    /// <param name="attribute">The marker attribute that requests the factory.</param>
    [TestMethod]
    [DataRow("DelimitedRecord")]
    [DataRow("IniSection")]
    public void Generator_WhenMemberTypesAreTheScalarsTheCodecLists_ShouldMapEveryOne(string attribute)
    {
        Type[] scalars = [.. InvariantScalarCodec.ScalarTypes, typeof(DayOfWeek)];
        var members = new StringBuilder();
        for (int i = 0; i < scalars.Length; i++)
        {
            string type = $"global::{scalars[i].FullName}";
            members.AppendLine(CultureInfo.InvariantCulture, $"    public {type} Value{i} {{ get; set; }} = default!;");
            if (scalars[i].IsValueType)
                members.AppendLine(CultureInfo.InvariantCulture, $"    public {type}? MaybeValue{i} {{ get; set; }}");
        }

        string source = $$"""
            #nullable enable
            using Bodu.Text.Delimited;
            using Bodu.Text.Ini;

            namespace Sample;

            [{{attribute}}]
            public sealed partial class Holder
            {
            {{members}}
            }
            """;

        (GeneratorDriverRunResult result, IReadOnlyList<Diagnostic> errors) = RunGeneratorAndCompile(source);

        Assert.IsTrue(InvariantScalarCodec.ScalarTypes.Count > 0);
        Assert.IsFalse(result.Diagnostics.Any(static d => d.Id == "BTFG002"), string.Join(Environment.NewLine, result.Diagnostics));
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }
}
