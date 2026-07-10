// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Represents a TOML configuration source — either a one-shot <see cref="System.IO.Stream" /> or a file path — that
/// produces a read-only <see cref="TomlConfigurationProvider" />.
/// </summary>
/// <remarks>
/// The source is read once when the configuration is built; it attaches no reload-on-change machinery. Exactly one of
/// <see cref="Stream" /> or <see cref="Path" /> should be set.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Normally added through the extension method:
/// IConfiguration configuration = new ConfigurationBuilder()
///     .AddTomlFile("appsettings.toml", optional: true)
///     .Build();
///
/// // The source itself is the seam for custom composition:
/// var source = new TomlConfigurationSource { Path = "appsettings.toml", Optional = true };
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class TomlConfigurationSource
    : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the stream containing UTF-8 TOML text. Takes precedence over <see cref="Path" /> when both are set.
    /// </summary>
    /// <value>The configured stream, or <see langword="null" /> when the source is file-backed.</value>
    public Stream? Stream { get; set; }

    /// <summary>
    /// Gets or sets the path of the TOML file to read.
    /// </summary>
    /// <value>The configured file path, or <see langword="null" /> when the source is stream-backed.</value>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a missing file at <see cref="Path" /> is permitted.
    /// </summary>
    /// <value><see langword="true" /> when the file may be absent; otherwise <see langword="false" />.</value>
    public bool Optional { get; set; }

    /// <summary>
    /// Builds the read-only provider for this source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>A <see cref="TomlConfigurationProvider" />.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new TomlConfigurationProvider(this);
}
