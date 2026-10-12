// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyMapping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how raw configuration keys are mapped to the logical keys used by the resolved view: colon-delimited, as
/// <c>Microsoft.Extensions.Configuration</c> expects, or, under <see cref="Identity" />, kept as written.
/// </summary>
/// <remarks>
/// A future release may add a <c>MixedDotAndColon</c> mode that permits a single document to combine both separator
/// forms; v1 deliberately keeps the surface area small.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // "logging.level" under each mapping:
/// //   DotToColon -> "logging:level" (dots and colons both address it)
/// //   Colon      -> "logging:level" (currently the same as DotToColon)
/// //   Identity   -> "logging.level" (verbatim)
/// var keyOptions = new ConfigurationKeyOptions { Mapping = ConfigurationKeyMapping.DotToColon };
///]]>
/// </code>
/// </example>
/// </remarks>
public enum ConfigurationKeyMapping
{
    /// <summary>
    /// Dotted file keys map to colon-delimited logical keys: <c>logging.level.default</c> ⇒
    /// <c>logging:level:default</c>. This is the default Bodu mapping and the primary v1 mode.
    /// </summary>
    DotToColon = 0,

    /// <summary>
    /// Intended for file keys that already use colon segment separators (<c>logging:level:default</c>), the MEC logical
    /// key shape. It currently behaves exactly as <see cref="DotToColon" />: a key is split on every configured
    /// separator, dots included, and its segments are joined with colons.
    /// </summary>
    Colon = 1,

    /// <summary>
    /// File keys are not split into segments, so their dots and colons stay as written, and the logical configuration
    /// key equals the raw key with the whitespace at its ends trimmed, lowercased when
    /// <see cref="ConfigurationKeyOptions.LowercaseKeys" /> is set. Use this when integrating with sources that use a
    /// custom segment convention; the <see cref="ConfigurationProfile.EditorConfigCompatible" /> presets use it.
    /// </summary>
    Identity = 2,
}
