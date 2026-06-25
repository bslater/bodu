// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlIgnoreAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Indicates that a property or field is excluded from YAML serialization and deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class YamlIgnoreAttribute : Attribute
{
}
