// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlKnownNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Identifies one of the built-in <see cref="TomlNamingPolicy" /> singletons by name, so a type can select a naming
/// policy declaratively through <see cref="TomlNamingPolicyAttribute" />.
/// </summary>
/// <remarks>
/// Each named value other than <see cref="Unspecified" /> corresponds to the like-named static property on
/// <see cref="TomlNamingPolicy" />.
/// </remarks>
public enum TomlKnownNamingPolicy
{
    /// <summary>
    /// No naming policy is selected; the member's CLR name or the options-level policy applies.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Selects <see cref="TomlNamingPolicy.CamelCase" />.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Selects <see cref="TomlNamingPolicy.SnakeCaseLower" />.
    /// </summary>
    SnakeCaseLower,

    /// <summary>
    /// Selects <see cref="TomlNamingPolicy.SnakeCaseUpper" />.
    /// </summary>
    SnakeCaseUpper,

    /// <summary>
    /// Selects <see cref="TomlNamingPolicy.KebabCaseLower" />.
    /// </summary>
    KebabCaseLower,

    /// <summary>
    /// Selects <see cref="TomlNamingPolicy.KebabCaseUpper" />.
    /// </summary>
    KebabCaseUpper,
}
