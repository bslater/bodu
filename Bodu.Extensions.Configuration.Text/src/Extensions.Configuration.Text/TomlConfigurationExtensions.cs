// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Provides extension methods for adding a read-only TOML configuration source to an
/// <see cref="IConfigurationBuilder" />, mirroring the <c>AddJsonFile</c> / <c>AddJsonStream</c> shape.
/// </summary>
/// <remarks>
/// The resulting provider is read-once and read-only - it attaches no reload-on-change machinery and rejects mutation
/// through <see cref="IConfigurationProvider" />, because <see cref="TomlConfigurationProvider" /> exposes no mutation
/// surface.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// builder.Configuration.AddTomlFile("appsettings.toml", optional: true);
///
/// using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[logging]\nlevel = \"Debug\""));
/// builder.Configuration.AddTomlStream(stream);
///]]>
/// </code>
/// </example>
public static class TomlConfigurationExtensions
{
    /// <summary>
    /// Adds a read-only TOML configuration source backed by the file at <paramref name="path" />.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The TOML file path, rooted or relative to the builder's file provider.</param>
    /// <param name="optional">When <see langword="true" />, the file is permitted to be missing.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// The path is located as <c>AddJsonFile</c> locates its file: a relative path through the builder's file provider,
    /// which is the provider <c>SetFileProvider</c> sets, the base path <c>SetBasePath</c> sets, or by default the
    /// application's base directory, and a rooted path from the file system. The default
    /// <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider" /> does not serve a file whose name begins
    /// with a dot; supply one constructed with <c>ExclusionFilters.None</c> through <c>SetFileProvider</c> to read one.
    /// </remarks>
    public static IConfigurationBuilder AddTomlFile(this IConfigurationBuilder builder, string path, bool optional = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        builder.Add(new TomlConfigurationSource { Path = path, Optional = optional });
        return builder;
    }

    /// <summary>
    /// Adds a read-only TOML configuration source backed by the supplied <see cref="Stream" />. The stream is read once
    /// when the configuration is built.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="stream">The stream containing UTF-8 TOML text.</param>
    /// <returns>The supplied <paramref name="builder" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public static IConfigurationBuilder AddTomlStream(this IConfigurationBuilder builder, Stream stream)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(stream);

        builder.Add(new TomlConfigurationSource { Stream = stream });
        return builder;
    }
}
