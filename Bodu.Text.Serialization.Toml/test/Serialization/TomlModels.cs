// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlModels.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// The logging level, used to exercise enum mapping.
/// </summary>
internal enum LogLevel
{
    /// <summary>
    /// Informational messages.
    /// </summary>
    Info,

    /// <summary>
    /// Warning messages.
    /// </summary>
    Warning,
}

/// <summary>
/// A flat configuration model with scalar members.
/// </summary>
internal sealed class ServerConfig
{
    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    /// <returns>The host name.</returns>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    /// <returns>The port.</returns>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether TLS is enabled.
    /// </summary>
    /// <returns>Whether TLS is enabled.</returns>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    /// <returns>The log level.</returns>
    public LogLevel Level { get; set; }
}

/// <summary>
/// A nested configuration model with a child table and a collection.
/// </summary>
internal sealed class AppConfig
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    /// <returns>The title.</returns>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server settings.
    /// </summary>
    /// <returns>The server settings.</returns>
    public ServerConfig Server { get; set; } = new();

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    /// <returns>The tags.</returns>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    /// <returns>The description.</returns>
    public string? Description { get; set; }
}

/// <summary>
/// A model carrying the date-time and binary kinds.
/// </summary>
internal sealed class EventRecord
{
    /// <summary>
    /// Gets or sets the instant.
    /// </summary>
    /// <returns>The instant.</returns>
    public DateTimeOffset When { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    /// <returns>The duration.</returns>
    public double Seconds { get; set; }

    /// <summary>
    /// Gets or sets the payload bytes.
    /// </summary>
    /// <returns>The payload.</returns>
    public byte[] Payload { get; set; } = [];
}
