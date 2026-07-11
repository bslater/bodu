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
/// The member must be a dictionary-shaped type keyed by <see cref="string" /> — the serializer's object node type, an
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />, or a
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> whose value is the serializer's node type. A type
/// may declare at most one extension-data member.
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
