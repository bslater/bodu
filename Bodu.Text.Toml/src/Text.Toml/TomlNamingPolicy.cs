// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Determines how a member's CLR name is translated to the dictionary key used in serialized TOML output. Mirrors
/// <see cref="System.Text.Json.JsonNamingPolicy" />, exposing the common casing conventions as ready-made policies.
/// </summary>
/// <remarks>
/// A naming policy applies only when a member does not carry an explicit
/// <see cref="Bodu.Text.Toml.Serialization.TomlPropertyNameAttribute" />, which always wins.
/// </remarks>
public abstract class TomlNamingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlNamingPolicy" /> class.
    /// </summary>
    protected TomlNamingPolicy()
    {
    }

    /// <summary>
    /// Gets a policy that converts a name to <c>camelCase</c> by lowercasing its first character.
    /// </summary>
    /// <returns>The camel-case naming policy.</returns>
    public static TomlNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a policy that converts a name to <c>snake_case</c> using lowercase letters.
    /// </summary>
    /// <returns>The lowercase snake-case naming policy.</returns>
    public static TomlNamingPolicy SnakeCaseLower { get; } = new SeparatorNamingPolicy('_', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>SNAKE_CASE</c> using uppercase letters.
    /// </summary>
    /// <returns>The uppercase snake-case naming policy.</returns>
    public static TomlNamingPolicy SnakeCaseUpper { get; } = new SeparatorNamingPolicy('_', toUpper: true);

    /// <summary>
    /// Gets a policy that converts a name to <c>kebab-case</c> using lowercase letters.
    /// </summary>
    /// <returns>The lowercase kebab-case naming policy.</returns>
    public static TomlNamingPolicy KebabCaseLower { get; } = new SeparatorNamingPolicy('-', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>KEBAB-CASE</c> using uppercase letters.
    /// </summary>
    /// <returns>The uppercase kebab-case naming policy.</returns>
    public static TomlNamingPolicy KebabCaseUpper { get; } = new SeparatorNamingPolicy('-', toUpper: true);

    /// <summary>
    /// Converts the specified member name to the dictionary key used in serialized output.
    /// </summary>
    /// <param name="name">The CLR member name.</param>
    /// <returns>The translated name.</returns>
    public abstract string ConvertName(string name);
}
