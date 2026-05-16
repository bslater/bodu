// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationProfile.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects one of the predefined behaviour profiles that govern how a configuration document is parsed,
/// resolved, and re-emitted.
/// </summary>
/// <remarks>
/// <para>
/// A profile is the single source of truth for an option set. Each
/// <see cref="BoduConfigurationParseOptions" />, <see cref="BoduConfigurationResolveOptions" />, and
/// <see cref="BoduConfigurationWriteOptions" /> exposes a <c>For(profile)</c> factory which produces an
/// options bag preconfigured for the selected profile.
/// </para>
/// </remarks>
public enum BoduConfigurationProfile
{
    /// <summary>
    /// The default Bodu profile: EditorConfig-style section headers and globs, dotted-to-colon key mapping,
    /// last-wins duplicate handling, whitespace-introduced inline comments, and preamble properties contributing
    /// to resolution.
    /// </summary>
    Bodu = 0,

    /// <summary>
    /// Strict alignment with the public EditorConfig specification 0.17.2: inline comments disabled, the
    /// preamble's <c>root</c> key alone participates in resolution, identity key mapping.
    /// </summary>
    EditorConfigCompatible = 1,

    /// <summary>
    /// Strict, deterministic parsing for generated files: duplicate keys are rejected, unknown syntax throws,
    /// inline comments are disabled, and key-only properties are not permitted.
    /// </summary>
    Strict = 2,

    /// <summary>
    /// Permissive parsing for user-authored files: inline comments enabled, duplicates resolved by last-wins,
    /// diagnostics collected rather than thrown.
    /// </summary>
    Relaxed = 3,
}
