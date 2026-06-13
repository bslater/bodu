// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProviderTests.Preservation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes.Test;

public partial class XmlDocFormatCodeFixProviderTests
{
    /// <summary>
    /// Verifies that attributes preceding a documented member survive the code fix.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenMemberHasAttribute_ShouldPreserveAttribute()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    [System.Obsolete]\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    [System.Obsolete]\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that multiple misformatted doc comments in the same file are each fixed when Fix All runs.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenMultipleDocsMisformatted_ShouldFixAll()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X;\r\n" +
            "    /// <summary>Bar.</summary>\r\n" +
            "    public int Y;\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "    /// <summary>\r\n" +
            "    /// Bar.\r\n" +
            "    /// </summary>\r\n" +
            "    public int Y;\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(5, 5, 6, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a <c>#pragma warning disable</c> directive between the doc comment and the documented
    /// member does not block the fix. The shape is taken verbatim from
    /// <c>Bodu.Core/src/Extensions/ArrayExtensions.Copy.cs</c>, where an <c>S1168</c> suppression sits
    /// directly above the <c>[return: MaybeNull]</c> attribute and the method declaration. A pragma never
    /// changes which member the doc comment attaches to, so it is not a barrier to the rewrite.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenPragmaBetweenDocAndMember_ShouldStillFix()
    {
        var source =
            "public static partial class ArrayExtensions\r\n" +
            "{\r\n" +
            "    /// <summary>Creates a shallow copy of the specified array of value types.</summary>\r\n" +
            "#pragma warning disable S1168\r\n" +
            "\r\n" +
            "    [return: System.Diagnostics.CodeAnalysis.MaybeNull]\r\n" +
            "    public static T[] Copy<T>(this T[] array)\r\n" +
            "        where T : struct => array is null ? null : (T[])array.Clone();\r\n" +
            "\r\n" +
            "#pragma warning restore S1168\r\n" +
            "}\r\n";

        var expected =
            "public static partial class ArrayExtensions\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Creates a shallow copy of the specified array of value types.\r\n" +
            "    /// </summary>\r\n" +
            "#pragma warning disable S1168\r\n" +
            "\r\n" +
            "    [return: System.Diagnostics.CodeAnalysis.MaybeNull]\r\n" +
            "    public static T[] Copy<T>(this T[] array)\r\n" +
            "        where T : struct => array is null ? null : (T[])array.Clone();\r\n" +
            "\r\n" +
            "#pragma warning restore S1168\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a <c>#region</c> directive between the doc comment and the documented member does
    /// not block the fix. Region directives are purely organisational and do not change which member the
    /// doc comment attaches to, so they sit alongside <c>#pragma</c>, <c>#nullable</c>, and <c>#line</c>
    /// on the safe-to-rewrite-around list.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenRegionDirectiveBetweenDocAndMember_ShouldStillFix()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    #region Helpers\r\n" +
            "    public int X { get; set; }\r\n" +
            "    #endregion\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    #region Helpers\r\n" +
            "    public int X { get; set; }\r\n" +
            "    #endregion\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a <c>#nullable</c> directive between the doc comment and the documented member does
    /// not block the fix. Nullable directives change the nullable annotation context but do not change
    /// which member the doc comment attaches to.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenNullableDirectiveBetweenDocAndMember_ShouldStillFix()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "#nullable enable\r\n" +
            "    public string? X { get; set; }\r\n" +
            "#nullable restore\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "#nullable enable\r\n" +
            "    public string? X { get; set; }\r\n" +
            "#nullable restore\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a conditional-compilation directive (<c>#if</c>/<c>#else</c>/<c>#endif</c>) between
    /// the doc comment and the documented member does not block the fix. The shape is taken verbatim
    /// from <c>Bodu.Core/src/Collections.Generic/EvictingDictionary.CacheItem.cs</c>, where the same doc
    /// comment attaches to either a <c>class</c> or a <c>record class</c> depending on target framework.
    /// The rewrite is a deterministic reformat driven by trivia content, indentation, and wrap width —
    /// none of which depend on which member the trivia attaches to — so replacing the trivia at its
    /// source position is safe across all configurations.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenConditionalDirectiveBetweenDocAndMember_ShouldStillFix()
    {
        var source =
            "public partial class EvictingDictionary<TKey, TValue>\r\n" +
            "{\r\n" +
            "    /// <summary>Represents a cache entry.</summary>\r\n" +
            "#if !NET5_0_OR_GREATER\r\n" +
            "\r\n" +
            "    private sealed class CacheItem { }\r\n" +
            "#else\r\n" +
            "    private sealed record class CacheItem { }\r\n" +
            "#endif\r\n" +
            "}\r\n";

        var expected =
            "public partial class EvictingDictionary<TKey, TValue>\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Represents a cache entry.\r\n" +
            "    /// </summary>\r\n" +
            "#if !NET5_0_OR_GREATER\r\n" +
            "\r\n" +
            "    private sealed class CacheItem { }\r\n" +
            "#else\r\n" +
            "    private sealed record class CacheItem { }\r\n" +
            "#endif\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the code fix does not change a canonical doc comment when run on a file that already
    /// satisfies the policy.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenAlreadyCanonical_ShouldMakeNoChanges()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }
}
