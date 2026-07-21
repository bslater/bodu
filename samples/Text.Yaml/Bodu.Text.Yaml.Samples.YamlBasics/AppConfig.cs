// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AppConfig.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics;

/// <summary>
/// The configuration POCO the scenarios round-trip. Property names map to YAML's snake_case keys
/// through the serializer's naming policy; the attributes mark the exceptions the policy cannot
/// express (an explicit wire name, a required key, and an ignored computed member).
/// </summary>
public sealed class AppConfig
{
    /// <summary>Gets or sets the service name (wire key <c>service_name</c> via the naming policy).</summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the retry budget.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets whether the service is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the sampling ratio — a YAML float scalar.</summary>
    public double SampleRate { get; set; }

    /// <summary>Gets or sets the database mapping (nested YAML mapping).</summary>
    public DatabaseConfig Database { get; set; } = new();

    /// <summary>Gets or sets the endpoint list (YAML block sequence of mappings).</summary>
    public List<EndpointConfig> Endpoints { get; set; } = [];

    /// <summary>Gets a computed display label — never serialized.</summary>
    [Ignore]
    public string DisplayLabel => $"{ServiceName} (retries: {MaxRetries})";
}

/// <summary>
/// The nested database mapping.
/// </summary>
public sealed class DatabaseConfig
{
    /// <summary>Gets or sets the host.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the port.</summary>
    public int Port { get; set; }
}

/// <summary>
/// One entry of the <c>endpoints</c> sequence. Shows an explicit wire-name override.
/// </summary>
public sealed class EndpointConfig
{
    /// <summary>Gets or sets the endpoint name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint address (wire key <c>url</c>, overriding the policy).</summary>
    [PropertyName("url")]
    public string Address { get; set; } = string.Empty;
}
