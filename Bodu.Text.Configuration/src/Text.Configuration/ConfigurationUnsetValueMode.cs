// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationUnsetValueMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how the resolver treats the literal value <c>unset</c>, which EditorConfig defines as a sentinel that
/// removes the effect of any previously set property for the matching path.
/// </summary>
/// <remarks>
/// In <see cref="ConfigurationProfile.EditorConfigCompatible" /> mode the resolver follows the EditorConfig rule and
/// removes the effective value. In <see cref="ConfigurationProfile.Bodu" /> mode the literal is preserved by default so
/// users who wish to store the string <c>unset</c> are not surprised. Set this mode explicitly when you want
/// EditorConfig-style behaviour under the Bodu profile.
/// </remarks>
public enum ConfigurationUnsetValueMode
{
    /// <summary>
    /// Treat <c>unset</c> as an ordinary literal value. The resolved view contains the string <c>"unset"</c> for the
    /// matching key.
    /// </summary>
    TreatAsLiteral = 0,

    /// <summary>
    /// Treat <c>unset</c> as a directive that removes the effective value of the property for the matching path. The
    /// resolved view omits the key entirely.
    /// </summary>
    RemoveEffectiveValue = 1,
}
