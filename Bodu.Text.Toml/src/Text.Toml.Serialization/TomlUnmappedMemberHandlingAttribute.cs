// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlUnmappedMemberHandlingAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Specifies, for the annotated type, how the serializer treats a dictionary key that maps to no member during
/// deserialization, overriding the serializer-wide <see cref="TomlSerializerOptions.UnmappedMemberHandling" />.
/// </summary>
/// <remarks>
/// When a type carries this attribute, its <see cref="UnmappedMemberHandling" /> is used in place of the options-level
/// default for that type. A type with an extension-data member still captures unmapped keys into that member regardless
/// of this setting.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [TomlUnmappedMemberHandling(TomlUnmappedMemberHandling.Disallow)]
/// public sealed class StrictConfig
/// {
///     public int Port { get; set; }
/// }
///
/// // Input containing a key that maps to no member throws TomlSerializationException.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class TomlUnmappedMemberHandlingAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlUnmappedMemberHandlingAttribute" /> class.
    /// </summary>
    /// <param name="unmappedMemberHandling">The handling applied to unmapped keys for the annotated type.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="unmappedMemberHandling" /> is not a defined
    /// <see cref="TomlUnmappedMemberHandling" /> value.
    /// </exception>
    public TomlUnmappedMemberHandlingAttribute(TomlUnmappedMemberHandling unmappedMemberHandling)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(unmappedMemberHandling);

        UnmappedMemberHandling = unmappedMemberHandling;
    }

    /// <summary>
    /// Gets the handling applied to unmapped keys for the annotated type.
    /// </summary>
    /// <returns>The unmapped-member handling.</returns>
    public TomlUnmappedMemberHandling UnmappedMemberHandling { get; }
}
