// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocument.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Multi-document stream parsing for <see cref="YamlDocument" />.
/// </summary>
public sealed partial class YamlDocument
{
    /// <summary>
    /// Parses every document in a multi-document YAML stream from UTF-8 source.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML stream.</param>
    /// <returns>The parsed documents, in stream order.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static IReadOnlyList<YamlDocument> ParseAllDocuments(ReadOnlySpan<byte> utf8Yaml) =>
        ParseAllDocuments(utf8Yaml, default);

    /// <summary>
    /// Parses every document in a multi-document YAML stream from UTF-8 source using the specified options.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML stream.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed documents, in stream order.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static IReadOnlyList<YamlDocument> ParseAllDocuments(ReadOnlySpan<byte> utf8Yaml, YamlDocumentOptions options)
    {
        var buffer = utf8Yaml.ToArray();
        var parser = new YamlParser(buffer, buffer.Length, options.SpecVersion, options.EffectiveMaxDepth, options.DuplicateKeyBehavior, options.MergeKeyBehavior);
        var documents = parser.ParseStream();

        var result = new List<YamlDocument>(documents.Count);
        foreach (var (rows, strings) in documents)
            result.Add(new YamlDocument(rows, strings));

        return result;
    }

    /// <summary>
    /// Parses every document in a multi-document YAML stream from a string.
    /// </summary>
    /// <param name="yaml">The YAML stream text.</param>
    /// <returns>The parsed documents, in stream order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static IReadOnlyList<YamlDocument> ParseAllDocuments(string yaml) =>
        ParseAllDocuments(yaml, default);

    /// <summary>
    /// Parses every document in a multi-document YAML stream from a string using the specified options.
    /// </summary>
    /// <param name="yaml">The YAML stream text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed documents, in stream order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static IReadOnlyList<YamlDocument> ParseAllDocuments(string yaml, YamlDocumentOptions options)
    {
        Bodu.ThrowHelper.ThrowIfNull(yaml);
        return ParseAllDocuments(Encoding.UTF8.GetBytes(yaml), options);
    }
}
