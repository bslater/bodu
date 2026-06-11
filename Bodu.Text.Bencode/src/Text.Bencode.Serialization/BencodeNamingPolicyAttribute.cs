// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNamingPolicyAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the <see cref="BencodeNamingPolicy" /> applied to the annotated type's members, overriding the
/// serializer-wide <see cref="BencodeSerializerOptions.PropertyNamingPolicy" /> for that type.
/// </summary>
/// <remarks>
/// The policy is chosen from the <see cref="BencodeKnownNamingPolicy" /> values, each of which maps to a static
/// singleton on <see cref="BencodeNamingPolicy" />. A member that carries an explicit
/// <see cref="BencodePropertyNameAttribute" /> is unaffected, since that attribute always takes precedence over any
/// naming policy.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class BencodeNamingPolicyAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeNamingPolicyAttribute" /> class.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy applied to the annotated type's members.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="namingPolicy" /> is not a defined <see cref="BencodeKnownNamingPolicy" /> value.
    /// </exception>
    public BencodeNamingPolicyAttribute(BencodeKnownNamingPolicy namingPolicy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(namingPolicy);

        NamingPolicy = Map(namingPolicy);
    }

    /// <summary>
    /// Gets the naming policy applied to the annotated type's members, or <see langword="null" /> when the attribute
    /// specifies <see cref="BencodeKnownNamingPolicy.Unspecified" />.
    /// </summary>
    /// <returns>The resolved naming policy, or <see langword="null" />.</returns>
    public BencodeNamingPolicy? NamingPolicy { get; }

    /// <summary>
    /// Maps a <see cref="BencodeKnownNamingPolicy" /> value to its corresponding <see cref="BencodeNamingPolicy" />
    /// singleton.
    /// </summary>
    /// <param name="namingPolicy">The known naming policy to map.</param>
    /// <returns>
    /// The matching <see cref="BencodeNamingPolicy" /> singleton, or <see langword="null" /> for
    /// <see cref="BencodeKnownNamingPolicy.Unspecified" />.
    /// </returns>
    private static BencodeNamingPolicy? Map(BencodeKnownNamingPolicy namingPolicy) =>
        namingPolicy switch
        {
            BencodeKnownNamingPolicy.CamelCase => BencodeNamingPolicy.CamelCase,
            BencodeKnownNamingPolicy.SnakeCaseLower => BencodeNamingPolicy.SnakeCaseLower,
            BencodeKnownNamingPolicy.SnakeCaseUpper => BencodeNamingPolicy.SnakeCaseUpper,
            BencodeKnownNamingPolicy.KebabCaseLower => BencodeNamingPolicy.KebabCaseLower,
            BencodeKnownNamingPolicy.KebabCaseUpper => BencodeNamingPolicy.KebabCaseUpper,
            _ => null,
        };
}
