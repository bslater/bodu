// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingPolicyAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the <see cref="Bodu.Text.Serialization.NamingPolicy" /> applied to the annotated type's members,
/// overriding the serializer-wide property naming policy for that type.
/// </summary>
/// <remarks>
/// The policy is chosen from the <see cref="KnownNamingPolicy" /> values, each of which maps to a static singleton on
/// <see cref="Bodu.Text.Serialization.NamingPolicy" />. A member that carries an explicit
/// <see cref="PropertyNameAttribute" /> is unaffected, since that attribute always takes precedence over any naming
/// policy.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]
/// public sealed class RetryPolicy
/// {
///     public int MaxRetryCount { get; set; } = 5;
/// }
///
/// // Serializes under the key "max_retry_count".
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class NamingPolicyAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamingPolicyAttribute" /> class.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy applied to the annotated type's members.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="namingPolicy" /> is not a defined <see cref="KnownNamingPolicy" /> value.
    /// </exception>
    public NamingPolicyAttribute(KnownNamingPolicy namingPolicy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(namingPolicy);

        NamingPolicy = Map(namingPolicy);
    }

    /// <summary>
    /// Gets the naming policy applied to the annotated type's members, or <see langword="null" /> when the attribute
    /// specifies <see cref="KnownNamingPolicy.Unspecified" />.
    /// </summary>
    /// <value>The resolved naming policy, or <see langword="null" />.</value>
    public NamingPolicy? NamingPolicy { get; }

    /// <summary>
    /// Maps a <see cref="KnownNamingPolicy" /> value to its corresponding
    /// <see cref="Bodu.Text.Serialization.NamingPolicy" /> singleton.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy to map.</param>
    /// <returns>
    /// The matching <see cref="Bodu.Text.Serialization.NamingPolicy" /> singleton, or <see langword="null" /> for
    /// <see cref="KnownNamingPolicy.Unspecified" />.
    /// </returns>
    private static NamingPolicy? Map(KnownNamingPolicy namingPolicy) =>
        namingPolicy switch
        {
            KnownNamingPolicy.CamelCase => Serialization.NamingPolicy.CamelCase,
            KnownNamingPolicy.SnakeCaseLower => Serialization.NamingPolicy.SnakeCaseLower,
            KnownNamingPolicy.SnakeCaseUpper => Serialization.NamingPolicy.SnakeCaseUpper,
            KnownNamingPolicy.KebabCaseLower => Serialization.NamingPolicy.KebabCaseLower,
            KnownNamingPolicy.KebabCaseUpper => Serialization.NamingPolicy.KebabCaseUpper,
            _ => null,
        };
}
