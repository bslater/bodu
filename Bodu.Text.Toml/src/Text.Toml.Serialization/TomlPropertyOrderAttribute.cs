// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlPropertyOrderAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Specifies the relative order in which a property or field is written, allowing the order members are emitted to the
/// writer to differ from the order in which they are declared.
/// </summary>
/// <remarks>
/// Members are written in ascending <see cref="Order" />. Members without the attribute take the default order of zero
/// and otherwise keep their declaration order. Because the TOML writer preserves the order in which members are
/// presented, this order governs the on-the-wire sequence of the table's key/value lines.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class TomlPropertyOrderAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPropertyOrderAttribute" /> class.
    /// </summary>
    /// <param name="order">The relative write order of the member.</param>
    public TomlPropertyOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the relative write order of the member.
    /// </summary>
    /// <returns>The order value; members are written in ascending order.</returns>
    public int Order { get; }
}
