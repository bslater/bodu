// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Provides convenience extension methods for adding a Bodu Text Configuration source to an
/// <see cref="IConfigurationBuilder" />.
/// </summary>
public static class BoduTextConfigurationExtensions
{
    /// <summary>
    /// Adds a Bodu Text Configuration source backed by the file at <paramref name="path" />.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The configuration file path, relative to the builder's file provider.</param>
    /// <param name="targetPath">The optional target path used for glob-anchored resolution.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <param name="reloadOnChange">When <see langword="true" />, the provider reloads the configuration when
    /// the underlying file changes.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is <see langword="null" />, empty, or
    /// whitespace.</exception>
    public static IConfigurationBuilder AddBoduConfiguration(
        this IConfigurationBuilder builder,
        string path,
        string? targetPath = null,
        bool optional = false,
        bool reloadOnChange = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        return builder.AddBoduConfiguration(source =>
        {
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
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or
    /// <paramref name="configureSource" /> is <see langword="null" />.</exception>
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
}
