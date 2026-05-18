// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Bodu.Text.Configuration;
using Bodu.Text.Formats;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Provides convenience extension methods for adding a Bodu Text Configuration source to an
/// <see cref="IConfigurationBuilder" />. The overload set mirrors <c>Microsoft.Extensions.Configuration.Json</c>'s
/// <c>AddJsonFile</c> / <c>AddJsonStream</c> shape so that consumers familiar with the JSON provider can swap in this
/// provider with no learning curve.
/// </summary>
public static class BoduTextConfigurationExtensions
{
    /// <summary>
    /// The conventional file name probed by <see cref="AddBoduConfiguration(IConfigurationBuilder, bool, bool)" />.
    /// </summary>
    private const string DefaultDotFileName = ".boduconfig";

    /// <summary>
    /// The alternative conventional file name probed by
    /// <see cref="AddBoduConfiguration(IConfigurationBuilder, bool, bool)" /> when <see cref="DefaultDotFileName" /> is
    /// absent.
    /// </summary>
    private const string DefaultPlainFileName = "bodu.config";

    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the file at <paramref name="path" />.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The configuration file path, relative to the builder's file provider.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <param name="reloadOnChange">
    /// When <see langword="true" />, the provider reloads the configuration when the underlying file changes.
    /// </param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        string path,
        string? targetPath = null,
        bool optional = false,
        bool reloadOnChange = false) =>
        builder.AddBoduConfiguration(provider: null, path, targetPath, optional, reloadOnChange);

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
    /// <param name="path">The configuration file path, relative to <paramref name="provider" />.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <param name="reloadOnChange">
    /// When <see langword="true" />, the provider reloads the configuration when the underlying file changes.
    /// </param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        IFileProvider? provider,
        string path,
        string? targetPath = null,
        bool optional = false,
        bool reloadOnChange = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        return builder.AddBoduConfiguration(source =>
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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="configureSource" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        Action<BoduTextConfigurationSource> configureSource)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureSource);

        BoduTextConfigurationSource source = new();
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
    public static IConfigurationBuilder AddBoduConfiguration(
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

        if (matched is null)
        {
            if (!optional)
            {
                throw new FileNotFoundException(
                    $"Neither '{DefaultDotFileName}' nor '{DefaultPlainFileName}' was found in the configured file provider.");
            }

            return builder.AddBoduConfiguration(source =>
            {
                source.FileProvider = fileProvider;
                source.Path = DefaultDotFileName;
                source.Optional = true;
                source.ReloadOnChange = reloadOnChange;
            });
        }

        return builder.AddBoduConfiguration(fileProvider, matched, targetPath: null, optional: optional, reloadOnChange: reloadOnChange);
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
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        Stream stream,
        string? targetPath = null,
        BoduConfigurationParseOptions? parseOptions = null,
        BoduConfigurationResolveOptions? resolveOptions = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(stream);

        return builder.AddBoduConfiguration(source =>
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
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        Action<BoduTextStreamConfigurationSource> configureSource)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureSource);

        BoduTextStreamConfigurationSource source = new();
        configureSource(source);
        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Adds an already-parsed <see cref="IniDocument" /> to the configuration. The document is resolved against
    /// <paramref name="targetPath" /> and the resulting key/value map is added via
    /// <see cref="MemoryConfigurationBuilderExtensions.AddInMemoryCollection" />.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="document">The pre-parsed configuration document.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="resolveOptions">The resolve options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        IniDocument document,
        string? targetPath = null,
        BoduConfigurationResolveOptions? resolveOptions = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(document);

        System.Collections.Generic.IDictionary<string, string?> data =
            BoduTextConfigurationLoader.LoadData(document, targetPath, resolveOptions);
        builder.AddInMemoryCollection(data);
        return builder;
    }
}
