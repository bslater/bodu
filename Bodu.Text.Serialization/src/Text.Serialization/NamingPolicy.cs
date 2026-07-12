// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Determines how a member's CLR name is translated to the key used in serialized output, exposing the common casing
/// conventions as ready-made policies.
/// </summary>
/// <remarks>
/// A naming policy applies only when a member does not carry an explicit <see cref="PropertyNameAttribute" />, which
/// always wins.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A MaxRetryCount property serializes under the key "max_retry_count".
/// var key = NamingPolicy.SnakeCaseLower.ConvertName("MaxRetryCount");
///]]>
/// </code>
/// </example>
public abstract class NamingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamingPolicy" /> class.
    /// </summary>
    protected NamingPolicy()
    {
    }

    /// <summary>
    /// Gets a policy that converts a name to <c>camelCase</c> by lowercasing its first character.
    /// </summary>
    /// <value>The camel-case naming policy.</value>
    public static NamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a policy that converts a name to <c>snake_case</c> using lowercase letters.
    /// </summary>
    /// <value>The lowercase snake-case naming policy.</value>
    public static NamingPolicy SnakeCaseLower { get; } = new SeparatorNamingPolicy('_', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>SNAKE_CASE</c> using uppercase letters.
    /// </summary>
    /// <value>The uppercase snake-case naming policy.</value>
    public static NamingPolicy SnakeCaseUpper { get; } = new SeparatorNamingPolicy('_', toUpper: true);

    /// <summary>
    /// Gets a policy that converts a name to <c>kebab-case</c> using lowercase letters.
    /// </summary>
    /// <value>The lowercase kebab-case naming policy.</value>
    public static NamingPolicy KebabCaseLower { get; } = new SeparatorNamingPolicy('-', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>KEBAB-CASE</c> using uppercase letters.
    /// </summary>
    /// <value>The uppercase kebab-case naming policy.</value>
    public static NamingPolicy KebabCaseUpper { get; } = new SeparatorNamingPolicy('-', toUpper: true);

    /// <summary>
    /// Converts the specified member name to the key used in serialized output.
    /// </summary>
    /// <param name="name">The CLR member name.</param>
    /// <returns>The translated name.</returns>
    public abstract string ConvertName(string name);
}
