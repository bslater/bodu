// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Yaml;

/// <summary>
/// A single <c>yaml-test-suite</c> vector loaded into memory: its identifier and description (the upstream <c>===</c>
/// file), its classification under the Bodu YAML Core Tree Profile, and the vector's input and expectation content.
/// </summary>
/// <remarks>
/// The vector mirrors the upstream layout — <c>in.yaml</c> is the input document and <c>in.json</c> is the canonical
/// JSON expectation when the suite publishes one. The <see cref="Category" /> is derived by
/// <see cref="YamlTestCorpusReader" /> from the case structure and the profile's by-name classification sets.
/// </remarks>
public sealed record YamlTestVector : IKat
{
    /// <summary>
    /// Gets the vector identifier, the path relative to the corpus root (for example <c>229Q</c> or <c>SM9W/00</c>).
    /// </summary>
    /// <value>The vector identifier.</value>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable description from the vector's upstream <c>===</c> file.
    /// </summary>
    /// <value>The description, or an empty string when none is present.</value>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the suite kind of the vector: <c>valid</c>, <c>valid-nojson</c>, or <c>invalid</c>.
    /// </summary>
    /// <value>The suite kind.</value>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets the profile classification of the vector as derived by <see cref="YamlTestCorpusReader" />.
    /// </summary>
    /// <value>The classification category, for example <c>SupportedPass</c>.</value>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the UTF-8 bytes of the vector's <c>in.yaml</c> input document.
    /// </summary>
    /// <value>The input document bytes.</value>
    public required byte[] Yaml { get; init; }

    /// <summary>
    /// Gets the UTF-8 bytes of the vector's <c>in.json</c> expectation, when the suite publishes one.
    /// </summary>
    /// <value>The expectation bytes, or <see langword="null" /> when the vector has no JSON expectation.</value>
    public byte[]? Json { get; init; }

    /// <inheritdoc />
    public string Name => string.IsNullOrEmpty(Description) ? Id : $"{Id} {Description}";
}
