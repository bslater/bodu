// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSyntaxTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Provides the entry point for parsing TOML text into a concrete syntax tree.
/// </summary>
/// <remarks>
/// The parsed document retains its exact source text, so <see cref="TomlDocumentSyntax.ToFullString" /> reproduces the
/// input without loss. The semantic table model exposed by the tree is what the serializer binds against.
/// </remarks>
public static class TomlSyntaxTree
{
    /// <summary>
    /// Parses TOML text into a document syntax tree.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="specVersion">The specification version whose grammar to enforce.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public static TomlDocumentSyntax Parse(ReadOnlySpan<char> source, TomlSpecVersion specVersion = TomlSpecVersion.V1_0)
    {
        EnsureValidScalarValues(source);

        var text = source.ToString();
        TomlParser parser = new(text, specVersion);
        TomlTableSyntax root = parser.Parse();
        return new TomlDocumentSyntax(root, text);
    }

    /// <summary>
    /// Validates that <paramref name="source" /> contains no unpaired UTF-16 surrogate before parsing begins.
    /// </summary>
    /// <param name="source">The source text to validate.</param>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> contains an unpaired surrogate.
    /// </exception>
    private static void EnsureValidScalarValues(ReadOnlySpan<char> source)
    {
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (c == '\n')
            {
                line++;
                lineStart = i + 1;
                continue;
            }

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= source.Length || !char.IsLowSurrogate(source[i + 1]))
                    throw new TomlFormatException(TomlSerializationResourceStrings.Format_Invalid_TomlUnpairedSurrogate, line, (i - lineStart) + 1, i);

                i++;
                continue;
            }

            if (char.IsLowSurrogate(c))
                throw new TomlFormatException(TomlSerializationResourceStrings.Format_Invalid_TomlUnpairedSurrogate, line, (i - lineStart) + 1, i);
        }
    }
}
