// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNamingPolicyAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Specifies the <see cref="TomlNamingPolicy" /> applied to the annotated type's members, overriding the
/// serializer-wide <see cref="TomlSerializerOptions.PropertyNamingPolicy" /> for that type.
/// </summary>
/// <remarks>
/// The policy is chosen from the <see cref="TomlKnownNamingPolicy" /> values, each of which maps to a static singleton
/// on <see cref="TomlNamingPolicy" />. A member that carries an explicit <see cref="TomlPropertyNameAttribute" /> is
/// unaffected, since that attribute always takes precedence over any naming policy.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [TomlNamingPolicy(TomlKnownNamingPolicy.SnakeCaseLower)]
/// public sealed class RetryPolicy
/// {
///     public int MaxRetryCount { get; set; } = 5;
/// }
///
/// // Serializes as: max_retry_count = 5
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class TomlNamingPolicyAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlNamingPolicyAttribute" /> class.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy applied to the annotated type's members.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="namingPolicy" /> is not a defined <see cref="TomlKnownNamingPolicy" /> value.
    /// </exception>
    public TomlNamingPolicyAttribute(TomlKnownNamingPolicy namingPolicy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(namingPolicy);

        NamingPolicy = Map(namingPolicy);
    }

    /// <summary>
    /// Gets the naming policy applied to the annotated type's members, or <see langword="null" /> when the attribute
    /// specifies <see cref="TomlKnownNamingPolicy.Unspecified" />.
    /// </summary>
    /// <value>The resolved naming policy, or <see langword="null" />.</value>
    public TomlNamingPolicy? NamingPolicy { get; }

    /// <summary>
    /// Maps a <see cref="TomlKnownNamingPolicy" /> value to its corresponding <see cref="TomlNamingPolicy" />
    /// singleton.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy to map.</param>
    /// <returns>
    /// The matching <see cref="TomlNamingPolicy" /> singleton, or <see langword="null" /> for
    /// <see cref="TomlKnownNamingPolicy.Unspecified" />.
    /// </returns>
    private static TomlNamingPolicy? Map(TomlKnownNamingPolicy namingPolicy) =>
        namingPolicy switch
        {
            TomlKnownNamingPolicy.CamelCase => TomlNamingPolicy.CamelCase,
            TomlKnownNamingPolicy.SnakeCaseLower => TomlNamingPolicy.SnakeCaseLower,
            TomlKnownNamingPolicy.SnakeCaseUpper => TomlNamingPolicy.SnakeCaseUpper,
            TomlKnownNamingPolicy.KebabCaseLower => TomlNamingPolicy.KebabCaseLower,
            TomlKnownNamingPolicy.KebabCaseUpper => TomlNamingPolicy.KebabCaseUpper,
            _ => null,
        };
}
