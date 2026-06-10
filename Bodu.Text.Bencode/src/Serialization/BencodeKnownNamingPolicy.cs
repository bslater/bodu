// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeKnownNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Identifies one of the built-in <see cref="BencodeNamingPolicy" /> singletons by name, so a type can select a naming
/// policy declaratively through <see cref="BencodeNamingPolicyAttribute" />. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonKnownNamingPolicy" />.
/// </summary>
/// <remarks>
/// Each named value other than <see cref="Unspecified" /> corresponds to the like-named static property on
/// <see cref="BencodeNamingPolicy" />.
/// </remarks>
public enum BencodeKnownNamingPolicy
{
    /// <summary>
    /// No naming policy is selected; the member's CLR name or the options-level policy applies.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Selects <see cref="BencodeNamingPolicy.CamelCase" />.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Selects <see cref="BencodeNamingPolicy.SnakeCaseLower" />.
    /// </summary>
    SnakeCaseLower,

    /// <summary>
    /// Selects <see cref="BencodeNamingPolicy.SnakeCaseUpper" />.
    /// </summary>
    SnakeCaseUpper,

    /// <summary>
    /// Selects <see cref="BencodeNamingPolicy.KebabCaseLower" />.
    /// </summary>
    KebabCaseLower,

    /// <summary>
    /// Selects <see cref="BencodeNamingPolicy.KebabCaseUpper" />.
    /// </summary>
    KebabCaseUpper,
}
