// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Determines how a member's CLR name is translated to the name used in serialized output. Mirrors
/// <see cref="System.Text.Json.JsonNamingPolicy" />, exposing the common casing conventions as ready-made policies.
/// </summary>
/// <remarks>
/// A naming policy applies only when a member does not carry an explicit <see cref="FormatPropertyNameAttribute" />,
/// which always wins.
/// </remarks>
public abstract class FormatNamingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatNamingPolicy" /> class.
    /// </summary>
    protected FormatNamingPolicy()
    {
    }

    /// <summary>
    /// Gets a policy that converts a name to <c>camelCase</c> by lowercasing its first character.
    /// </summary>
    /// <returns>The camel-case naming policy.</returns>
    public static FormatNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a policy that converts a name to <c>snake_case</c> using lowercase letters.
    /// </summary>
    /// <returns>The lowercase snake-case naming policy.</returns>
    public static FormatNamingPolicy SnakeCaseLower { get; } = new SeparatorNamingPolicy('_', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>SNAKE_CASE</c> using uppercase letters.
    /// </summary>
    /// <returns>The uppercase snake-case naming policy.</returns>
    public static FormatNamingPolicy SnakeCaseUpper { get; } = new SeparatorNamingPolicy('_', toUpper: true);

    /// <summary>
    /// Gets a policy that converts a name to <c>kebab-case</c> using lowercase letters.
    /// </summary>
    /// <returns>The lowercase kebab-case naming policy.</returns>
    public static FormatNamingPolicy KebabCaseLower { get; } = new SeparatorNamingPolicy('-', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>KEBAB-CASE</c> using uppercase letters.
    /// </summary>
    /// <returns>The uppercase kebab-case naming policy.</returns>
    public static FormatNamingPolicy KebabCaseUpper { get; } = new SeparatorNamingPolicy('-', toUpper: true);

    /// <summary>
    /// Converts the specified member name to the name used in serialized output.
    /// </summary>
    /// <param name="name">The CLR member name.</param>
    /// <returns>The translated name.</returns>
    public abstract string ConvertName(string name);
}
