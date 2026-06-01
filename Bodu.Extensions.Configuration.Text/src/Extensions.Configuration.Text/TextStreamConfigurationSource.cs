// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextStreamConfigurationSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="StreamConfigurationSource" /> that reads a Bodu Text Configuration document from an arbitrary
/// <see cref="System.IO.Stream" /> and projects its resolved view into the <see cref="IConfiguration" /> hierarchy as
/// colon-delimited keys.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the role of <c>JsonStreamConfigurationSource</c> in <c>Microsoft.Extensions.Configuration.Json</c>. Unlike
/// <see cref="TextConfigurationSource" /> (which is file-backed and inherits reload-on-change), this source is
/// one-shot: the stream is parsed once when <see cref="Build(IConfigurationBuilder)" /> is invoked and no file watcher
/// is attached. The caller is responsible for the stream's lifetime; the provider does not dispose it.
/// </para>
/// <para>
/// Use this shape when the configuration text lives somewhere other than the file system — embedded resources,
/// in-memory test inputs, content downloaded from a config service, or content authored by another part of the host
/// process. <see cref="TargetPath" />, <see cref="ParseOptions" />, and <see cref="ResolveOptions" /> behave the same
/// way as on the file-backed source.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Test-time configuration from an in-memory string.
/// const string ConfigText = """
///     [*]
///     Logging:Level = Debug
///
///     [src/**/*.cs]
///     format:indent:size = 4
///     """;
///
/// using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ConfigText));
///
/// IConfigurationRoot root = new ConfigurationBuilder()
///     .AddConfiguration(source =>
///     {
///         source.Stream     = stream;
///         source.TargetPath = "src/Foo.cs";
///     })
///     .Build();
///
/// Console.WriteLine(root["Logging:Level"]);          // "Debug"
/// Console.WriteLine(root["format:indent:size"]);     // "4"
///]]>
/// </example>
public sealed class TextStreamConfigurationSource : StreamConfigurationSource
{
    /// <summary>
    /// Gets or sets the path used to evaluate glob-anchored sections during resolution. Defaults to
    /// <see langword="null" />, in which case only non-anchored matches and preamble values apply.
    /// </summary>
    /// <returns>The target path supplied to the resolver.</returns>
    public string? TargetPath { get; set; }

    /// <summary>
    /// Gets or sets the parse options applied when the stream is loaded.
    /// </summary>
    /// <returns>The parse options, or <see langword="null" /> for the defaults.</returns>
    public ConfigurationParseOptions? ParseOptions { get; set; }

    /// <summary>
    /// Gets or sets the resolve options applied when projecting the document into the configuration view.
    /// </summary>
    /// <returns>The resolve options, or <see langword="null" /> for the defaults.</returns>
    public ConfigurationResolveOptions? ResolveOptions { get; set; }

    /// <inheritdoc />
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        return new TextStreamConfigurationProvider(this);
    }
}
