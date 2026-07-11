// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Serves as the base class for the attributes that customize how a type or member is mapped to and from a text format
/// by the serializer.
/// </summary>
/// <remarks>
/// This base type lets the serialization attributes be discovered and reasoned about as a single family.
/// </remarks>
public abstract class SerializationAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationAttribute" /> class.
    /// </summary>
    protected SerializationAttribute()
    {
    }
}
