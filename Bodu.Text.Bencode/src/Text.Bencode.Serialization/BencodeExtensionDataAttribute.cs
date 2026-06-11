// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeExtensionDataAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Designates a property or field that captures dictionary entries which do not map to any other member during
/// deserialization, and whose entries are written back out during serialization.
/// </summary>
/// <remarks>
/// <para>
/// The member must be of type <see cref="Bodu.Text.Bencode.Nodes.BencodeObject" />,
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />, or
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> with a key of <see cref="string" /> and a value
/// of <see cref="Bodu.Text.Bencode.Nodes.BencodeNode" />. A type may declare at most one extension-data member.
/// </para>
/// <para>
/// On serialization the captured entries are written alongside the type's other members; because the Bencode writer
/// re-sorts dictionary entries into canonical ascending key order, the extension entries merge into the correct
/// position automatically.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class BencodeExtensionDataAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeExtensionDataAttribute" /> class.
    /// </summary>
    public BencodeExtensionDataAttribute()
    {
    }
}
