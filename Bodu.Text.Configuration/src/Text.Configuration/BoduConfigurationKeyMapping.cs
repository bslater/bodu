// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyMapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how raw configuration keys are mapped to the colon-delimited logical keys used by the resolved
/// view and by <c>Microsoft.Extensions.Configuration</c>.
/// </summary>
/// <remarks>
/// A future release may add a <c>MixedDotAndColon</c> mode that permits a single document to combine both
/// separator forms; v1 deliberately keeps the surface area small.
/// </remarks>
public enum BoduConfigurationKeyMapping
{
    /// <summary>
    /// Dotted file keys map to colon-delimited logical keys: <c>logging.level.default</c> ⇒
    /// <c>logging:level:default</c>. This is the default Bodu mapping and the primary v1 mode.
    /// </summary>
    DotToColon = 0,

    /// <summary>
    /// File keys already use colon segment separators (<c>logging:level:default</c>) and are emitted as-is.
    /// Provided for callers whose input already matches the MEC logical key shape.
    /// </summary>
    Colon = 1,

    /// <summary>
    /// File keys are emitted unchanged. The logical configuration key equals the raw key. Use this when
    /// integrating with sources that use a custom segment convention.
    /// </summary>
    Identity = 2,
}
