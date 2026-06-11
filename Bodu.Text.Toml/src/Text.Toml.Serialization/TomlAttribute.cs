// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Serves as the base class for the attributes that customize how a type or member is mapped to and from TOML by the
/// serializer.
/// </summary>
/// <remarks>
/// This base type lets the TOML serialization attributes be discovered and reasoned about as a single family.
/// </remarks>
public abstract class TomlAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlAttribute" /> class.
    /// </summary>
    protected TomlAttribute()
    {
    }
}
