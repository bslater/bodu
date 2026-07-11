// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AppConfig.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml.Samples.TomlBasics;

/// <summary>
/// The configuration POCO the scenarios round-trip. Property names map to TOML's snake_case keys
/// through the serializer's naming policy; the attributes mark the exceptions the policy cannot
/// express (an explicit wire name, a required key, and an ignored computed member).
/// </summary>
public sealed class AppConfig
{
    /// <summary>Gets or sets the service name (wire key <c>service_name</c> via the naming policy).</summary>
    [TomlRequired]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the retry budget.</summary>
    public int MaxRetries { get; set; }

    /// <summary>Gets or sets whether the service is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the release date — a TOML local date (no time, no offset).</summary>
    public DateOnly ReleasedOn { get; set; }

    /// <summary>Gets or sets the maintenance window start — a TOML local time.</summary>
    public TimeOnly MaintenanceWindow { get; set; }

    /// <summary>Gets or sets the build stamp — a TOML offset date-time (exact instant).</summary>
    public DateTimeOffset BuildStamp { get; set; }

    /// <summary>Gets or sets the database table (nested TOML table).</summary>
    public DatabaseConfig Database { get; set; } = new();

    /// <summary>Gets or sets the endpoint list (TOML array of tables).</summary>
    public List<EndpointConfig> Endpoints { get; set; } = [];

    /// <summary>Gets a computed display label — never serialized.</summary>
    [TomlIgnore]
    public string DisplayLabel => $"{ServiceName} (retries: {MaxRetries})";
}

/// <summary>
/// The nested database table.
/// </summary>
public sealed class DatabaseConfig
{
    /// <summary>Gets or sets the host.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the port.</summary>
    public int Port { get; set; }
}

/// <summary>
/// One entry of the <c>[[endpoints]]</c> array of tables. Shows an explicit wire-name override.
/// </summary>
public sealed class EndpointConfig
{
    /// <summary>Gets or sets the endpoint name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint address (wire key <c>url</c>, overriding the policy).</summary>
    [TomlPropertyName("url")]
    public string Address { get; set; } = string.Empty;
}
