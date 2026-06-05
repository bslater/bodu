// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IPluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Decides whether a candidate plugin assembly is trusted, evaluated before any of the plugin's code is activated.
/// </summary>
public interface IPluginTrustPolicy
{
    /// <summary>
    /// Evaluates the trust of a candidate plugin assembly.
    /// </summary>
    /// <param name="context">The metadata describing the candidate assembly.</param>
    /// <returns>The trust result.</returns>
    PluginTrustResult Evaluate(PluginTrustContext context);
}
