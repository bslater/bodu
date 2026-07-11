// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KnownNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Identifies one of the built-in <see cref="NamingPolicy" /> singletons by name, so a type can select a naming policy
/// declaratively through <see cref="NamingPolicyAttribute" />.
/// </summary>
/// <remarks>
/// Each named value other than <see cref="Unspecified" /> corresponds to the like-named static property on
/// <see cref="NamingPolicy" />.
/// </remarks>
public enum KnownNamingPolicy
{
    /// <summary>
    /// No naming policy is selected; the member's CLR name or the options-level policy applies.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Selects <see cref="NamingPolicy.CamelCase" />.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Selects <see cref="NamingPolicy.SnakeCaseLower" />.
    /// </summary>
    SnakeCaseLower,

    /// <summary>
    /// Selects <see cref="NamingPolicy.SnakeCaseUpper" />.
    /// </summary>
    SnakeCaseUpper,

    /// <summary>
    /// Selects <see cref="NamingPolicy.KebabCaseLower" />.
    /// </summary>
    KebabCaseLower,

    /// <summary>
    /// Selects <see cref="NamingPolicy.KebabCaseUpper" />.
    /// </summary>
    KebabCaseUpper,
}
