// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Toml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Toml;

/// <summary>
/// Provides convenience entry points for parsing and formatting TOML text, delegating to the <see cref="TomlReader" />
/// and <see cref="TomlWriter" /> types that own deserialization and serialization respectively. The document structure
/// itself is the <see cref="TomlTable" /> / <see cref="TomlValue" /> model.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/toml-pipeline.svg" alt="The TOML parse/format pipeline runs in both directions. Parsing takes TOML source text, runs TomlReader to tokenize and apply the grammar, validates the spec rules (the v1.0.0 or v1.1.0 version, no duplicate keys, no table redefinition), and returns a strongly typed TomlTable. Formatting takes a TomlTable, runs TomlWriter to walk the model in insertion order, canonicalizes the layout into block-style tables with inline scalars and arrays, and writes canonical TOML text valid under both spec versions."/>
/// </para>
/// <para>
/// These static helpers exist for ergonomic one-liners; the read/write pair (<see cref="TomlReader" /> and
/// <see cref="TomlWriter" />) is the primary surface, mirroring the relationship between <c>XmlReader</c> /
/// <c>XmlWriter</c> and their document model.
/// </para>
/// <para>
/// The parameterless parse methods enforce strict TOML v1.0.0. The overloads that accept a
/// <see cref="TomlReaderOptions" /> allow opting in to TOML v1.1.0 grammar via
/// <see cref="TomlReaderOptions.SpecVersion" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// TomlTable doc = Toml.Parse("""
///     title = "example"
///
///     [owner]
///     name = "Tom"
///     dob = 1979-05-27T07:32:00Z
///     """);
///
/// string text = Toml.Format(doc);
///]]>
/// </example>
public static class Toml
{
    /// <summary>
    /// The shared, stateless reader used by the convenience parse methods.
    /// </summary>
    private static readonly TomlReader s_reader = new();

    /// <summary>
    /// The shared, stateless writer used by the convenience format methods.
    /// </summary>
    private static readonly TomlWriter s_writer = new();

    /// <summary>
    /// Parses a TOML document from the specified character span.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public static TomlTable Parse(ReadOnlySpan<char> source) =>
        s_reader.Read(source);

    /// <summary>
    /// Parses a TOML document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="options">The options that govern parsing, including the specification version.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public static TomlTable Parse(ReadOnlySpan<char> source, TomlReaderOptions options) =>
        new TomlReader(options).Read(source);

    /// <summary>
    /// Parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    public static TomlTable Parse(Stream source) =>
        s_reader.Read(source);

    /// <summary>
    /// Parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text, using the supplied
    /// options.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The options that govern parsing, including the specification version.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    public static TomlTable Parse(Stream source, TomlReaderOptions options) =>
        new TomlReader(options).Read(source);

    /// <summary>
    /// Asynchronously parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    public static ValueTask<TomlTable> ParseAsync(Stream source, CancellationToken cancellationToken = default) =>
        s_reader.ReadAsync(source, cancellationToken);

    /// <summary>
    /// Asynchronously parses a TOML document by reading <paramref name="source" /> to its end as UTF-8 text, using the
    /// supplied options.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="options">The options that govern parsing, including the specification version.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    public static ValueTask<TomlTable> ParseAsync(Stream source, TomlReaderOptions options, CancellationToken cancellationToken = default) =>
        new TomlReader(options).ReadAsync(source, cancellationToken);

    /// <summary>
    /// Attempts to parse a TOML document from the specified character span.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, the parsed document; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> source, [NotNullWhen(true)] out TomlTable? document) =>
        s_reader.TryRead(source, out document);

    /// <summary>
    /// Attempts to parse a TOML document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="options">The options that govern parsing, including the specification version.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, the parsed document; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public static bool TryParse(ReadOnlySpan<char> source, TomlReaderOptions options, [NotNullWhen(true)] out TomlTable? document) =>
        new TomlReader(options).TryRead(source, out document);

    /// <summary>
    /// Formats a <see cref="TomlTable" /> document to canonical TOML text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>The TOML representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static string Format(TomlTable document) =>
        s_writer.Write(document);

    /// <summary>
    /// Formats <paramref name="document" /> and writes the result to <paramref name="destination" /> as UTF-8 text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="destination">The writable stream that receives the UTF-8 bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    public static void Format(TomlTable document, Stream destination) =>
        s_writer.Write(document, destination);

    /// <summary>
    /// Asynchronously formats <paramref name="document" /> and writes the result to <paramref name="destination" /> as
    /// UTF-8 text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="destination">The writable stream that receives the UTF-8 bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the write.</param>
    /// <returns>A task that completes once the encoded bytes have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the write completes.
    /// </exception>
    public static ValueTask FormatAsync(TomlTable document, Stream destination, CancellationToken cancellationToken = default) =>
        s_writer.WriteAsync(document, destination, cancellationToken);
}
