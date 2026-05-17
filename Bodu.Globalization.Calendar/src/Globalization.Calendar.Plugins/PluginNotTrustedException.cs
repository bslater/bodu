// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginNotTrustedException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when the configured <see cref="IPluginTrustPolicy" /> rejects a
/// candidate plugin assembly before it is loaded into the process.
/// </summary>
public sealed class PluginNotTrustedException
    : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotTrustedException" /> class.
    /// </summary>
    public PluginNotTrustedException()
        : base()
    {
        AssemblyPath = string.Empty;
        Reason = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotTrustedException" /> class with the specified error
    /// message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PluginNotTrustedException(string message)
        : base(message)
    {
        AssemblyPath = string.Empty;
        Reason = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotTrustedException" /> class with the specified error
    /// message and a reference to the inner exception that caused this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PluginNotTrustedException(string message, Exception innerException)
        : base(message, innerException)
    {
        AssemblyPath = string.Empty;
        Reason = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotTrustedException" /> class for a plugin assembly rejected
    /// by a trust policy.
    /// </summary>
    /// <param name="assemblyPath">The path of the rejected plugin assembly.</param>
    /// <param name="reason">
    /// The trust policy's stated reason for rejecting the plugin assembly, or <see langword="null" /> when the policy
    /// did not supply one.
    /// </param>
    /// <remarks>
    /// This constructor preserves the rejected assembly path and optional trust-policy reason so callers can inspect
    /// the failed trust evaluation in addition to the formatted exception message.
    /// </remarks>
    public PluginNotTrustedException(string assemblyPath, string? reason)
        : base($"Plugin assembly '{assemblyPath}' was rejected by the trust policy: {reason ?? "no reason supplied"}.")
    {
        AssemblyPath = assemblyPath;
        Reason = reason;
    }

    /// <summary>
    /// Gets the path of the rejected plugin assembly.
    /// </summary>
    /// <value>
    /// The filesystem path supplied at construction, or <see cref="string.Empty" /> when the exception was created
    /// without plugin-assembly context.
    /// </value>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets the reason surfaced by the trust policy.
    /// </summary>
    /// <value>
    /// The trust policy's stated reason for rejection, or <see langword="null" /> when the policy did not supply one
    /// or when the exception was created without trust-policy context.
    /// </value>
    public string? Reason { get; }
}
