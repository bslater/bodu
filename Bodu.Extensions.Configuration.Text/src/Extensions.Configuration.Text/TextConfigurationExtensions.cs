// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Provides convenience extension methods for adding a Bodu Text Configuration source to an
/// <see cref="IConfigurationBuilder" />. The overload set mirrors <c>Microsoft.Extensions.Configuration.Json</c>'s
/// <c>AddJsonFile</c> / <c>AddJsonStream</c> shape so that consumers familiar with the JSON provider can swap in this
/// provider with no learning curve.
/// </summary>
/// <remarks>
/// <para>
/// Five overload shapes are available, mirroring the JSON provider's surface:
/// <list type="bullet">
/// <item>
/// <description>File path with optional reload-on-change - the everyday production shape.</description>
/// </item>
/// <item>
/// <description>
/// File path with an explicit <see cref="IFileProvider" /> - useful when the file lives outside the default content
/// root, in an embedded assembly resource, or under a virtual file system.
/// </description>
/// </item>
/// <item>
/// <description>
/// Lambda configure-source overload - the most flexible shape, exposes every <see cref="TextConfigurationSource" />
/// property.
/// </description>
/// </item>
/// <item>
/// <description>
/// Convention-discovery overload - probes for <c>.boduconfig</c>, then <c>bodu.config</c>, in that order.
/// </description>
/// </item>
/// <item>
/// <description>
/// Stream overload (and a lambda <see cref="TextStreamConfigurationSource" /> variant) - one-shot, no reload-on-change
/// machinery; ideal for tests and synthetic configuration.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// File-backed registrations honour the standard <see cref="FileConfigurationSource" /> behaviours (reload-on-change,
/// optional-file, exception wrapping), resolve a relative <c>path</c> through the supplied or builder-default
/// <see cref="IFileProvider" />, and, when no provider is supplied, read a rooted <c>path</c> from the file system, as
/// <c>AddJsonFile</c> does. The convention overload uses the builder's default <see cref="IFileProvider" />; note that
/// <see cref="IFileProvider" /> implementations such as
/// <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider" /> filter out dot-prefixed files by default -
/// see the remarks on <see cref="AddTextConfiguration(IConfigurationBuilder, bool, bool)" /> for the workaround
/// required to surface <c>.boduconfig</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // 1. Canonical ASP.NET / Generic Host registration in Program.cs.
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Configuration
///     .AddTextConfigurationFile("appsettings.boduconfig", optional: true, reloadOnChange: true)
///     .AddTextConfigurationFile("appsettings.boduconfig", targetPath: "src/Foo.cs"); // path-aware view
///
/// // 2. Convention discovery - probes .boduconfig, then bodu.config.
/// builder.Configuration.AddTextConfiguration(optional: true, reloadOnChange: true);
///
/// // 3. Lambda overload - pin every option, including parse/resolve behaviour.
/// builder.Configuration.AddTextConfigurationFile(source =>
/// {
///     source.Path           = "app.boduconfig";
///     source.TargetPath     = "src/Web/Startup.cs";
///     source.Optional       = false;
///     source.ReloadOnChange = true;
///     source.ParseOptions   = ConfigurationParseOptions.Strict;
///     source.ResolveOptions = ConfigurationResolveOptions.Bodu;
/// });
///
/// // 4. In-memory stream - handy in unit tests. With no target path only the preamble applies, so the key sits
/// //    before any section.
/// using var ms = new MemoryStream(Encoding.UTF8.GetBytes("Logging:Level=Debug\n"));
/// builder.Configuration.AddTextConfigurationStream(ms);
///
/// // 5. Pre-parsed document - share one parse across multiple builders.
/// ConfigurationDocument doc = ConfigurationDocument.Parse(text);
/// builder.Configuration.AddTextConfigurationDocument(doc, targetPath: "src/Foo.cs");
///]]>
/// </code>
/// </example>
public static class TextConfigurationExtensions
{
    /// <summary>The conventional file name probed by <see cref="AddTextConfiguration(IConfigurationBuilder, bool, bool)" />.</summary>
    private const string DefaultDotFileName = ".boduconfig";

    /// <summary>The alternative conventional file name probed by <see cref="AddTextConfiguration(IConfigurationBuilder, bool, bool)" /> when <see cref="DefaultDotFileName" /> is absent.</summary>
    private const string DefaultPlainFileName = "bodu.config";

    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the file at <paramref name="path" />.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The configuration file path, rooted or relative to the builder's file provider.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <param name="reloadOnChange">
    /// When <see langword="true" />, the provider reloads the configuration when the underlying file changes.
    /// </param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <remarks>
    /// The file is located as <c>AddJsonFile</c> locates it: a rooted path is read from the file system, and any other
    /// path through the builder's file provider, which is the provider <c>SetFileProvider</c> sets, the base path
    /// <c>SetBasePath</c> sets, or by default the application's base directory.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationFile(
        this IConfigurationBuilder builder,
        string path,
        string? targetPath = null,
        bool optional = false,
        bool reloadOnChange = false) =>
        builder.AddTextConfigurationFile(provider: null, path, targetPath, optional, reloadOnChange);

    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the file at <paramref name="path" /> using the supplied
    /// <see cref="IFileProvider" />. Mirrors
    /// <c>AddJsonFile(IConfigurationBuilder, IFileProvider, string, bool, bool)</c>.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="provider">
    /// The file provider that locates <paramref name="path" />, or <see langword="null" /> to defer to the builder's
    /// default file provider.
    /// </param>
    /// <param name="path">
    /// The configuration file path, relative to <paramref name="provider" />, or, when no provider is supplied, rooted
    /// or relative to the builder's file provider.
    /// </param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <param name="reloadOnChange">
    /// When <see langword="true" />, the provider reloads the configuration when the underlying file changes.
    /// </param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <remarks>
    /// With <paramref name="provider" /> set to <see langword="null" />, the file is located as <c>AddJsonFile</c>
    /// locates it: a rooted path is read from the file system, and any other path through the builder's file provider.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationFile(
        this IConfigurationBuilder builder,
        IFileProvider? provider,
        string path,
        string? targetPath = null,
        bool optional = false,
        bool reloadOnChange = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        return builder.AddTextConfigurationFile(source =>
        {
            source.FileProvider = provider;
            source.Path = path;
            source.TargetPath = targetPath;
            source.Optional = optional;
            source.ReloadOnChange = reloadOnChange;
        });
    }

    /// <summary>
    /// Adds a Bodu Text Configuration source configured via the supplied callback.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configureSource">A callback that configures the source.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <remarks>
    /// The source locates the file as <c>AddJsonFile</c> does, a rooted path included; see
    /// <see cref="TextConfigurationSource.Build(IConfigurationBuilder)" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="configureSource" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationFile(
        this IConfigurationBuilder builder,
        Action<TextConfigurationSource> configureSource)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureSource);

        TextConfigurationSource source = new();
        configureSource(source);
        builder.Add(source);

        return builder;
    }

    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the conventional file name <c>.boduconfig</c>, falling back to
    /// <c>bodu.config</c> when the dot-prefixed name is absent. The file is resolved against the builder's default file
    /// provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="optional">
    /// When <see langword="true" /> (the default), neither file is required to exist; when <see langword="false" />, at
    /// least one of the two conventional names must resolve.
    /// </param>
    /// <param name="reloadOnChange">
    /// When <see langword="true" />, the provider reloads the configuration when the underlying file changes.
    /// </param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <remarks>
    /// The default <c>PhysicalFileProvider</c> filters out dot-prefixed files via its <c>ExclusionFilters</c> (
    /// <c>Sensitive</c> by default), so to make <c>.boduconfig</c> resolvable the caller must register a
    /// <c>PhysicalFileProvider</c> constructed with <c>ExclusionFilters.None</c>. The fallback <c>bodu.config</c> name
    /// is resolved by the default exclusion filters without further configuration.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="FileNotFoundException">
    /// Both conventional files are absent and <paramref name="optional" /> is <see langword="false" />.
    /// </exception>
    public static IConfigurationBuilder AddTextConfiguration(
        this IConfigurationBuilder builder,
        bool optional = true,
        bool reloadOnChange = false)
    {
        ThrowHelper.ThrowIfNull(builder);

        IFileProvider fileProvider = builder.GetFileProvider();
        string? matched = null;

        if (fileProvider.GetFileInfo(DefaultDotFileName).Exists)
            matched = DefaultDotFileName;
        else if (fileProvider.GetFileInfo(DefaultPlainFileName).Exists)
            matched = DefaultPlainFileName;

        return matched is not null
            ? builder.AddTextConfigurationFile(fileProvider, matched, targetPath: null, optional: optional, reloadOnChange: reloadOnChange)
            : optional
                ? builder.AddTextConfigurationFile(source =>
                {
                    source.FileProvider = fileProvider;
                    source.Path = DefaultDotFileName;
                    source.Optional = true;
                    source.ReloadOnChange = reloadOnChange;
                })
                : throw new FileNotFoundException(
                    string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.IO_FileNotFound_DefaultConfigFiles, DefaultDotFileName, DefaultPlainFileName));
    }

    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the supplied <see cref="Stream" />. The stream is read once when
    /// the configuration is built; no reload-on-change machinery is attached. Mirrors
    /// <c>AddJsonStream(IConfigurationBuilder, Stream)</c>.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="stream">The stream containing configuration text.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="parseOptions">The parse options, or <see langword="null" /> for the defaults.</param>
    /// <param name="resolveOptions">The resolve options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationStream(
        this IConfigurationBuilder builder,
        Stream stream,
        string? targetPath = null,
        ConfigurationParseOptions? parseOptions = null,
        ConfigurationResolveOptions? resolveOptions = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(stream);

        return builder.AddTextConfigurationStream(source =>
        {
            source.Stream = stream;
            source.TargetPath = targetPath;
            source.ParseOptions = parseOptions;
            source.ResolveOptions = resolveOptions;
        });
    }

    /// <summary>
    /// Adds a Bodu Text Configuration stream source configured via the supplied callback.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configureSource">A callback that configures the source.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="configureSource" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationStream(
        this IConfigurationBuilder builder,
        Action<TextStreamConfigurationSource> configureSource)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureSource);

        TextStreamConfigurationSource source = new();
        configureSource(source);
        builder.Add(source);

        return builder;
    }

    /// <summary>
    /// Adds an already-parsed <see cref="IniDocumentBase" /> (such as a <see cref="ConfigurationDocument" />) to the
    /// configuration. The document is resolved against <paramref name="targetPath" /> and the resulting key/value map
    /// is added via
    /// <see cref="MemoryConfigurationBuilderExtensions.AddInMemoryCollection(IConfigurationBuilder, IEnumerable{KeyValuePair{string, string?}}?)" />
    /// .
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="document">The pre-parsed configuration document.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="resolveOptions">The resolve options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <remarks>
    /// This overload takes a <b>one-shot snapshot</b> of <paramref name="document" /> as it stands when called: the
    /// document is resolved immediately and the flattened values are added in-memory. Unlike the file-based overloads,
    /// it has <b>no reload-on-change</b> behaviour - subsequent edits to the document or its backing file are not
    /// reflected in the built configuration.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddTextConfigurationDocument(
        this IConfigurationBuilder builder,
        IniDocumentBase document,
        string? targetPath = null,
        ConfigurationResolveOptions? resolveOptions = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(document);

        System.Collections.Generic.IDictionary<string, string?> data =
            TextConfigurationLoader.LoadData(document, targetPath, resolveOptions);

        builder.AddInMemoryCollection(data);

        return builder;
    }
}
