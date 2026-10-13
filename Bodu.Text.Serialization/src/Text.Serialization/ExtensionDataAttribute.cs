// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionDataAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Designates a property or field that captures dictionary entries which do not map to any other member during
/// deserialization, and whose entries are written back out during serialization.
/// </summary>
/// <remarks>
/// <para>
/// The member must be a dictionary-shaped type keyed by <see cref="string" />, and the accepted types differ by
/// serializer. For Bencode and TOML it is the format's object node type (<c>BencodeObject</c> or <c>TomlObject</c>), or
/// an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> or
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> whose value is the format's node type. For YAML
/// it is any type that implements <c>IDictionary&lt;string, object?&gt;</c>, such as
/// <c>Dictionary&lt;string, object?&gt;</c>. A type may declare at most one extension-data member.
/// </para>
/// <para>
/// On serialization the captured entries are written alongside the type's other members.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ExtensionDataAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionDataAttribute" /> class.
    /// </summary>
    public ExtensionDataAttribute()
    {
    }
}
