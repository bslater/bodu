// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlRequiredAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Marks a property or field as required, so deserialization fails when the corresponding key is absent from the input.
/// </summary>
/// <remarks>
/// Applying this attribute has the same effect as declaring the member with the <see langword="required" /> keyword:
/// the member must appear in the TOML table being read, otherwise a <see cref="TomlSerializationException" /> is
/// thrown.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlRequiredAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlRequiredAttribute" /> class.
    /// </summary>
    public TomlRequiredAttribute()
    {
    }
}
