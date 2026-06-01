// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Text;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Provides the profile-aware entry points for parsing, loading, and saving Bodu Text Configuration documents. The
/// underlying data model is <see cref="IniDocument" /> from <c>Bodu.Text.Ini</c>; this class adds Bodu-specific
/// behaviour — profile presets, inline-comment-mode handling, diagnostic routing, and round-trip support — on top of
/// the shared INI infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Pair the <see cref="Parse(string)" /> family with the
/// <see cref="ConfigurationExtensions.Resolve(IniDocument, string?, ConfigurationResolveOptions?)" /> extension method
/// to evaluate the document for a specific target path. The document holds the raw, source-faithful model;
/// <see cref="ConfigurationView" /> holds the resolved, target-specific snapshot consumed by application code.
/// </para>
/// <para>
/// Diagnostic routing is controlled by the supplied <see cref="ConfigurationParseOptions" />. In
/// <see cref="ConfigurationDiagnosticMode.Throw" /> mode a recoverable parse error raises
/// <see cref="ConfigurationParseException" /> at the first issue; in <see cref="ConfigurationDiagnosticMode.Collect" />
/// mode the parser continues, the document's valid portions remain usable, and every diagnostic surfaces on
/// <see cref="ConfigurationParseResult.Diagnostics" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse and resolve a configuration file for a specific target path.
/// IniDocument         doc    = ConfigurationDocument.Parse(text);
/// ConfigurationView view = doc.Resolve("src/Foo.cs");
/// int indent = view.GetInt32("format:indent:size", 4);
///
/// Collect every diagnostic instead of failing on the first issue.
/// var opts = new ConfigurationParseOptions
/// {
///     Profile        = ConfigurationProfile.Bodu,
///     DiagnosticMode = ConfigurationDiagnosticMode.Collect,
/// };
/// ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(text, opts);
/// foreach (ConfigurationDiagnostic d in result.Diagnostics)
///     Console.WriteLine(d);
///]]>
/// </example>
public static class ConfigurationDocument
{
    /// <summary>
    /// Parses configuration text using the default Bodu profile.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationParseException">The text could not be parsed.</exception>
    public static IniDocument Parse(string text) =>
        Parse(text, options: null);

    /// <summary>
    /// Parses configuration text using the supplied options.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the Bodu defaults.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationParseException">The text could not be parsed.</exception>
    public static IniDocument Parse(string text, ConfigurationParseOptions? options)
    {
        ThrowHelper.ThrowIfNull(text);

        using StringReader reader = new(text);
        return new ConfigurationReader(options ?? ConfigurationParseOptions.Bodu)
            .Read(reader, path: null)
            .Document;
    }

    /// <summary>
    /// Parses configuration text and returns the resulting document together with any diagnostics collected during the
    /// parse.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the Bodu defaults.</param>
    /// <returns>The parse result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationParseException">
    /// The text could not be parsed and <see cref="ConfigurationParseOptions.DiagnosticMode" /> is
    /// <see cref="ConfigurationDiagnosticMode.Throw" />.
    /// </exception>
    public static ConfigurationParseResult ParseWithDiagnostics(string text, ConfigurationParseOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        using StringReader reader = new(text);
        return new ConfigurationReader(options ?? ConfigurationParseOptions.Bodu).Read(reader, path: null);
    }

    /// <summary>
    /// Attempts to parse configuration text using the default Bodu profile.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="document">The parsed document on success; <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? text, out IniDocument? document) =>
        TryParse(text, options: null, out document);

    /// <summary>
    /// Attempts to parse configuration text using the supplied options.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="options">The parse options, or <see langword="null" /> for the Bodu defaults.</param>
    /// <param name="document">The parsed document on success; <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? text, ConfigurationParseOptions? options, out IniDocument? document)
    {
        if (text is null)
        {
            document = null;
            return false;
        }

        try
        {
            document = Parse(text, options);
            return true;
        }
        catch (ConfigurationParseException)
        {
            document = null;
            return false;
        }
    }

    /// <summary>
    /// Loads a configuration document from the file at <paramref name="path" /> using the default Bodu profile and
    /// UTF-8 encoding with BOM detection.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is empty or whitespace.</exception>
    public static IniDocument Load(string path) =>
        Load(path, options: null);

    /// <summary>
    /// Loads a configuration document from the file at <paramref name="path" /> using the supplied options.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The parse options, or <see langword="null" /> for the Bodu defaults.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is empty or whitespace.</exception>
    public static IniDocument Load(string path, ConfigurationParseOptions? options)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        ConfigurationParseOptions effective = options ?? ConfigurationParseOptions.Bodu;
        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new(stream, effective.DefaultEncoding, detectEncodingFromByteOrderMarks: true);
        return new ConfigurationReader(effective).Read(reader, path).Document;
    }

    /// <summary>
    /// Loads a configuration document from the supplied stream.
    /// </summary>
    /// <param name="stream">The stream to read from. Must support reading.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <param name="encoding">
    /// The encoding to use when the stream lacks a byte order mark, or <see langword="null" /> to use the parse
    /// options' default encoding.
    /// </param>
    /// <param name="leaveOpen">When <see langword="true" />, the stream remains open after parsing.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream" /> does not support reading.</exception>
    public static IniDocument Load(
        Stream stream,
        ConfigurationParseOptions? options = null,
        Encoding? encoding = null,
        bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        ConfigurationThrowHelper.ThrowIfStreamNotReadable(stream);

        ConfigurationParseOptions effective = options ?? ConfigurationParseOptions.Bodu;
        Encoding effectiveEncoding = encoding ?? effective.DefaultEncoding;

        using StreamReader reader = new(stream, effectiveEncoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: leaveOpen);
        return new ConfigurationReader(effective).Read(reader, path: null).Document;
    }

    /// <summary>
    /// Loads a configuration document from the supplied text reader.
    /// </summary>
    /// <param name="reader">The reader to consume.</param>
    /// <param name="options">The parse options, or <see langword="null" /> for the defaults.</param>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader" /> is <see langword="null" />.</exception>
    public static IniDocument Load(TextReader reader, ConfigurationParseOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(reader);
        return new ConfigurationReader(options ?? ConfigurationParseOptions.Bodu).Read(reader, path: null).Document;
    }

    /// <summary>
    /// Saves <paramref name="document" /> to the file at <paramref name="path" />, replacing any existing content.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The write options, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document" /> or <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is empty or whitespace.</exception>
    public static void Save(IniDocument document, string path, ConfigurationWriteOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        ConfigurationWriteOptions effective = options ?? ConfigurationWriteOptions.Bodu;
        using FileStream stream = File.Create(path);
        using StreamWriter writer = new(stream, effective.Encoding);
        ConfigurationDocumentWriter.Write(document, writer, effective);
    }

    /// <summary>
    /// Saves <paramref name="document" /> to the supplied stream.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="stream">The destination stream. Must support writing.</param>
    /// <param name="options">The write options, or <see langword="null" /> for the defaults.</param>
    /// <param name="leaveOpen">When <see langword="true" />, the stream remains open after writing.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document" /> or <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="stream" /> does not support writing.</exception>
    public static void Save(IniDocument document, Stream stream, ConfigurationWriteOptions? options = null, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(stream);
        ConfigurationThrowHelper.ThrowIfStreamNotWritable(stream);

        ConfigurationWriteOptions effective = options ?? ConfigurationWriteOptions.Bodu;
        using StreamWriter writer = new(stream, effective.Encoding, bufferSize: 1024, leaveOpen: leaveOpen);
        ConfigurationDocumentWriter.Write(document, writer, effective);
    }

    /// <summary>
    /// Saves <paramref name="document" /> to the supplied text writer.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">The write options, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document" /> or <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static void Save(IniDocument document, TextWriter writer, ConfigurationWriteOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(writer);

        ConfigurationDocumentWriter.Write(document, writer, options ?? ConfigurationWriteOptions.Bodu);
    }
}

/// <summary>
/// Carries the outcome of a configuration parse: the populated <see cref="IniDocument" /> and any diagnostics collected
/// during the parse.
/// </summary>
/// <remarks>
/// Under <see cref="ConfigurationDiagnosticMode.Throw" /> the parser raises <see cref="ConfigurationParseException" />
/// on the first recoverable error, so a returned <see cref="ConfigurationParseResult" /> from
/// <see cref="ConfigurationDocument.ParseWithDiagnostics" /> always represents a successful parse whose diagnostic list
/// is empty. Under <see cref="ConfigurationDiagnosticMode.Collect" /> the parser runs to completion and the diagnostic
/// list reflects every recoverable error and warning encountered.
/// </remarks>
public sealed class ConfigurationParseResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseResult" /> class.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <param name="diagnostics">The diagnostics collected during the parse.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    public ConfigurationParseResult(IniDocument document, ImmutableArray<ConfigurationDiagnostic> diagnostics)
    {
        ThrowHelper.ThrowIfNull(document);
        Document = document;
        Diagnostics = diagnostics.IsDefault ? [] : diagnostics;
    }

    /// <summary>
    /// Gets the parsed document.
    /// </summary>
    /// <returns>A populated <see cref="IniDocument" />.</returns>
    public IniDocument Document { get; }

    /// <summary>
    /// Gets the diagnostics collected during the parse.
    /// </summary>
    /// <returns>An immutable, possibly empty array of diagnostics.</returns>
    public ImmutableArray<ConfigurationDiagnostic> Diagnostics { get; }
}
