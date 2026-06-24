// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Determines how a member's CLR name is translated to the dictionary key used in serialized Bencode output, exposing
/// the common casing conventions as ready-made policies.
/// </summary>
/// <remarks>
/// A naming policy applies only when a member does not carry an explicit
/// <see cref="Bodu.Text.Bencode.Serialization.BencodePropertyNameAttribute" />, which always wins.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.SnakeCaseLower };
///
/// // A MaxRetryCount property now serializes under the dictionary key: 15:max_retry_count
///]]>
/// </code>
/// </example>
public abstract class BencodeNamingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeNamingPolicy" /> class.
    /// </summary>
    protected BencodeNamingPolicy()
    {
    }

    /// <summary>
    /// Gets a policy that converts a name to <c>camelCase</c> by lowercasing its first character.
    /// </summary>
    /// <value>The camel-case naming policy.</value>
    public static BencodeNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a policy that converts a name to <c>snake_case</c> using lowercase letters.
    /// </summary>
    /// <value>The lowercase snake-case naming policy.</value>
    public static BencodeNamingPolicy SnakeCaseLower { get; } = new SeparatorNamingPolicy('_', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>SNAKE_CASE</c> using uppercase letters.
    /// </summary>
    /// <value>The uppercase snake-case naming policy.</value>
    public static BencodeNamingPolicy SnakeCaseUpper { get; } = new SeparatorNamingPolicy('_', toUpper: true);

    /// <summary>
    /// Gets a policy that converts a name to <c>kebab-case</c> using lowercase letters.
    /// </summary>
    /// <value>The lowercase kebab-case naming policy.</value>
    public static BencodeNamingPolicy KebabCaseLower { get; } = new SeparatorNamingPolicy('-', toUpper: false);

    /// <summary>
    /// Gets a policy that converts a name to <c>KEBAB-CASE</c> using uppercase letters.
    /// </summary>
    /// <value>The uppercase kebab-case naming policy.</value>
    public static BencodeNamingPolicy KebabCaseUpper { get; } = new SeparatorNamingPolicy('-', toUpper: true);

    /// <summary>
    /// Converts the specified member name to the dictionary key used in serialized output.
    /// </summary>
    /// <param name="name">The CLR member name.</param>
    /// <returns>The translated name.</returns>
    public abstract string ConvertName(string name);
}
