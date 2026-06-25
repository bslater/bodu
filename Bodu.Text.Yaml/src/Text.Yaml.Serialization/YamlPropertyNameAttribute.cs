// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlPropertyNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Specifies the YAML key used for a property or field, overriding the member name and any naming policy.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class YamlPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The YAML key to use for the annotated member.</param>
    public YamlPropertyNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the YAML key used for the annotated member.
    /// </summary>
    /// <value>The key name.</value>
    public string Name { get; }
}
