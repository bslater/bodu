// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2.Plugins;

/// <summary>
/// Carries the metadata a trust policy evaluates before a plugin assembly's code is activated.
/// </summary>
/// <param name="AssemblyName">The simple name of the candidate assembly.</param>
/// <param name="AssemblyPath">The file path of the candidate assembly, or <see langword="null" /> when loaded in memory.</param>
/// <param name="FileHash">The SHA-256 hash of the assembly file, or <see langword="null" /> when no file is available.</param>
public sealed record PluginTrustContext(
    string AssemblyName,
    string? AssemblyPath,
    byte[]? FileHash);
