// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.SharedSources.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that Bodu.Text.Bencode compiles only the shared serializer sources it uses, so that a shipping build, which
/// withdraws the test assemblies' access to its internals, raises no analyzer finding on code the package never runs.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// The prefix of the names under which the test project embeds a copy of each line-format shared source, from
    /// <c>Bodu.Text.Serialization/flat/src</c>.
    /// </summary>
    private const string LineFormatSharedSourcePrefix = "LineFormatSharedSources/";

    /// <summary>
    /// Gets the full name of every type the line-format shared sources in <c>Bodu.Text.Serialization/flat/src</c>
    /// declare, read from the copies the test project embeds, so that a type added there is covered without a change
    /// here.
    /// </summary>
    /// <value>The rows, each the full name of a type, with a generic arity suffix where it has type parameters.</value>
    public static IEnumerable<object[]> LineFormatSharedTypeNames =>
        typeof(BencodeSerializerTests).Assembly.GetManifestResourceNames()
            .Where(static name => name.StartsWith(LineFormatSharedSourcePrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .SelectMany(ReadDeclaredTypeNames)
            .Select(static typeName => new object[] { typeName });

    /// <summary>
    /// Verifies that the Bodu.Text.Bencode assembly holds none of the types the <c>flat/src</c> sources declare for the
    /// line formats, Bodu.Text.Delimited, Bodu.Text.DotEnv and Bodu.Text.Ini, none of which it uses.
    /// </summary>
    /// <param name="typeName">The full name of a type the <c>flat/src</c> sources declare.</param>
    [TestMethod]
    [DynamicData(nameof(LineFormatSharedTypeNames))]
    public void Assembly_WhenBuilt_ShouldNotCompileLineFormatSharedSources(string typeName)
    {
        Type? type = typeof(BencodeSerializer).Assembly.GetType(typeName, throwOnError: false);

        Assert.IsNull(type, typeName);
    }

    /// <summary>
    /// Reads the full names of the top-level types an embedded line-format shared source declares.
    /// </summary>
    /// <param name="resourceName">The name of the embedded source.</param>
    /// <returns>The full name of each type declared directly in a namespace, in declaration order.</returns>
    private static IEnumerable<string> ReadDeclaredTypeNames(string resourceName)
    {
        using Stream stream = typeof(BencodeSerializerTests).Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Where(static declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)
            .Select(static declaration =>
            {
                string name = $"{((BaseNamespaceDeclarationSyntax)declaration.Parent!).Name}.{declaration.Identifier.ValueText}";
                int arity = (declaration as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Count ?? 0;

                return arity == 0 ? name : $"{name}`{arity}";
            })
            .ToList();
    }
}
