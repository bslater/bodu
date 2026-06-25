// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Provides options that configure <see cref="YamlSerializer" />, in the manner of
/// <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new YamlSerializerOptions
/// {
///     PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower,
///     SpecVersion = YamlSpecVersion.V1_1,
/// };
/// string yaml = YamlSerializer.Serialize(config, options);
///]]>
/// </code>
/// </example>
public sealed class YamlSerializerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializerOptions" /> class.
    /// </summary>
    public YamlSerializerOptions()
    {
        Converters = new Collection<YamlConverter>();
    }

    /// <summary>
    /// Gets the list of custom converters consulted before the built-in handling.
    /// </summary>
    /// <value>The converter collection.</value>
    public Collection<YamlConverter> Converters { get; }

    /// <summary>
    /// Gets or sets the policy used to convert member names to YAML keys.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names verbatim.</value>
    public YamlNamingPolicy? PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether public fields are serialized in addition to properties.
    /// </summary>
    /// <value><see langword="true" /> to include public fields; otherwise <see langword="false" />.</value>
    public bool IncludeFields { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether members with a null value are omitted when writing.
    /// </summary>
    /// <value><see langword="true" /> to omit null members; otherwise <see langword="false" />.</value>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enumerations are written as their string names.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to write enum names; <see langword="false" /> to write their numeric values. The default
    /// is <see langword="true" />.
    /// </value>
    public bool WriteEnumsAsStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets the property-name comparison used when binding YAML keys to members.
    /// </summary>
    /// <value><see langword="true" /> to match keys case-insensitively; otherwise <see langword="false" />.</value>
    public bool PropertyNameCaseInsensitive { get; set; }

    /// <summary>
    /// Gets or sets the YAML specification version applied when parsing during deserialization.
    /// </summary>
    /// <value>The specification version; the default is <see cref="YamlSpecVersion.V1_2" />.</value>
    public YamlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Finds a custom converter for the specified type.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>The matching converter, or <see langword="null" /> when none applies.</returns>
    internal YamlConverter? GetConverter(Type type)
    {
        foreach (var converter in Converters)
        {
            if (converter.CanConvert(type))
                return converter;
        }

        return null;
    }
}
