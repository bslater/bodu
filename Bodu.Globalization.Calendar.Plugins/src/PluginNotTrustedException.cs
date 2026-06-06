// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginNotTrustedException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The exception thrown when a trust policy rejects a candidate plugin assembly.
/// </summary>
public sealed class PluginNotTrustedException : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginNotTrustedException" /> class.
    /// </summary>
    /// <param name="message">The message describing the rejection.</param>
    /// <param name="assemblyName">The name of the rejected assembly.</param>
    /// <param name="reason">The reason the assembly was rejected, if supplied.</param>
    public PluginNotTrustedException(string message, string assemblyName, string? reason)
        : base(message)
    {
        AssemblyName = assemblyName;
        Reason = reason;
    }

    /// <summary>
    /// Gets the name of the rejected assembly.
    /// </summary>
    /// <returns>The assembly name.</returns>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the reason the assembly was rejected.
    /// </summary>
    /// <returns>The rejection reason, or <see langword="null" /> when none was supplied.</returns>
    public string? Reason { get; }
}
