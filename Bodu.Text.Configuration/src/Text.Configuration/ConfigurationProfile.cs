// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationProfile.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects one of the predefined behaviour profiles that govern how a configuration document is parsed, resolved, and
/// re-emitted.
/// </summary>
/// <remarks>
/// <para>
/// A profile is the single source of truth for an option set. Each <see cref="ConfigurationParseOptions" />,
/// <see cref="ConfigurationResolveOptions" />, and <see cref="ConfigurationWriteOptions" /> exposes a
/// <c>For(profile)</c> factory which produces an options bag preconfigured for the selected profile.
/// </para>
/// <para>
/// Profiles encode opinionated combinations of orthogonal switches. Mixing them is supported — start from the
/// closest preset and override the few properties that differ. The behavioural matrix is:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Behaviour</term>
/// <description>Bodu | EditorConfigCompatible | Strict | Relaxed</description>
/// </listheader>
/// <item>
/// <term>Inline comments (<see cref="ConfigurationInlineCommentMode" />)</term>
/// <description>WhitespaceIntroduced | Disabled | Disabled | WhitespaceIntroduced</description>
/// </item>
/// <item>
/// <term>Duplicate keys (<see cref="Bodu.Text.Ini.IniDuplicateKeyBehavior" />)</term>
/// <description>LastWins | LastWins | Disallowed | LastWins</description>
/// </item>
/// <item>
/// <term>Duplicate sections (<see cref="Bodu.Text.Ini.IniDuplicateSectionBehavior" />)</term>
/// <description>Preserve | Preserve | Disallowed | Preserve</description>
/// </item>
/// <item>
/// <term>Section header trailing content (<see cref="ConfigurationSectionHeaderMode" />)</term>
/// <description>Lenient | Strict | Strict | Lenient</description>
/// </item>
/// <item>
/// <term>Diagnostic routing (<see cref="ConfigurationDiagnosticMode" />)</term>
/// <description>Throw | Throw | Throw | Collect</description>
/// </item>
/// <item>
/// <term>Preamble contributes to resolved view (<see cref="ConfigurationResolveOptions.ApplyPreambleProperties" />)</term>
/// <description>Yes | No | Yes | Yes</description>
/// </item>
/// <item>
/// <term><c>unset</c> sentinel (<see cref="ConfigurationUnsetValueMode" />)</term>
/// <description>TreatAsLiteral | RemoveEffectiveValue | RemoveEffectiveValue | TreatAsLiteral</description>
/// </item>
/// <item>
/// <term>Missing path root (<see cref="ConfigurationMissingPathRootMode" />)</term>
/// <description>UseEmptyRoot | Throw | Throw | UseEmptyRoot</description>
/// </item>
/// </list>
/// </remarks>
public enum ConfigurationProfile
{
    /// <summary>
    /// The default Bodu profile: EditorConfig-style section headers and globs, dotted-to-colon key mapping, last-wins
    /// duplicate handling, whitespace-introduced inline comments, and preamble properties contributing to resolution.
    /// </summary>
    Bodu = 0,

    /// <summary>
    /// Strict alignment with the public EditorConfig specification 0.17.2: inline comments disabled, the preamble's
    /// <c>root</c> key alone participates in resolution, identity key mapping.
    /// </summary>
    EditorConfigCompatible = 1,

    /// <summary>
    /// Strict, deterministic parsing for generated files: duplicate keys are rejected, unknown syntax throws, inline
    /// comments are disabled, and key-only properties are not permitted.
    /// </summary>
    Strict = 2,

    /// <summary>
    /// Permissive parsing for user-authored files: inline comments enabled, duplicates resolved by last-wins,
    /// diagnostics collected rather than thrown.
    /// </summary>
    Relaxed = 3,
}
