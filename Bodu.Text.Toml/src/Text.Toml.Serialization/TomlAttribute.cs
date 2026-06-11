// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Serves as the base class for the attributes that customize how a type or member is mapped to and from TOML by the
/// serializer. Mirrors the role of <see cref="System.Text.Json.Serialization.JsonAttribute" />.
/// </summary>
/// <remarks>
/// This base type lets the TOML serialization attributes be discovered and reasoned about as a single family, matching
/// the way <see cref="System.Text.Json.Serialization.JsonAttribute" /> groups the <c>System.Text.Json</c> attributes.
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
