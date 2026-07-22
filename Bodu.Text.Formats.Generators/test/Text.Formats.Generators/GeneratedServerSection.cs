// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedServerSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;
using Bodu.Text.Serialization;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// An INI section POCO whose <c>IniFactory</c> is emitted by the source generator at build time, covering string,
/// numeric, boolean-renamed, and nullable keys.
/// </summary>
[IniSection]
public sealed partial class GeneratedServerSection
{
    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    /// <value>The host name.</value>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    /// <value>The port.</value>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether TLS is used, mapped to the <c>use_tls</c> key.
    /// </summary>
    /// <value><see langword="true" /> to use TLS.</value>
    [PropertyName("use_tls")]
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets the optional idle timeout.
    /// </summary>
    /// <value>The timeout, or <see langword="null" />.</value>
    public TimeSpan? Timeout { get; set; }
}
