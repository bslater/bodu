// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatPropertyOrderAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the relative order in which a property or field is written, allowing the serialized layout to differ from
/// the order in which members are declared.
/// </summary>
/// <remarks>
/// Members are written in ascending <see cref="Order" />. Members without the attribute take the default order of zero
/// and otherwise keep their declaration order. This mirrors
/// <see cref="System.Text.Json.Serialization.JsonPropertyOrderAttribute" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class FormatPropertyOrderAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatPropertyOrderAttribute" /> class.
    /// </summary>
    /// <param name="order">The relative write order of the member.</param>
    public FormatPropertyOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the relative write order of the member.
    /// </summary>
    /// <returns>The order value; members are written in ascending order.</returns>
    public int Order { get; }
}
