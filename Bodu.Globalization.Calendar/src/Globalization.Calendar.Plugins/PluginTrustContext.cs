// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustContext.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Carries the material an <see cref="IPluginTrustPolicy" /> needs to decide whether a candidate plugin assembly may be
/// loaded.
/// </summary>
/// <param name="AssemblyPath">The absolute filesystem path to the plugin assembly.</param>
/// <param name="AssemblyName">
/// The assembly name metadata, including any public-key token a signed assembly carries.
/// </param>
/// <param name="FileHash">
/// The SHA-256 hash of the plugin assembly's bytes, computed by the loader before any code from the assembly runs.
/// Never <see langword="null" />.
/// </param>
public readonly record struct PluginTrustContext(
    string AssemblyPath,
    AssemblyName AssemblyName,
    byte[] FileHash);
