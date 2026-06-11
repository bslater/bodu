// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodePropertyOrderAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the relative order in which a property or field is written, allowing the order members are emitted to the
/// writer to differ from the order in which they are declared.
/// </summary>
/// <remarks>
/// Members are written in ascending <see cref="Order" />. Members without the attribute take the default order of zero
/// and otherwise keep their declaration order. Because the Bencode writer re-sorts dictionary entries into canonical
/// ascending key order when a dictionary is closed, this order governs the sequence in which members are presented to
/// the writer rather than the final on-the-wire order.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class BencodePropertyOrderAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodePropertyOrderAttribute" /> class.
    /// </summary>
    /// <param name="order">The relative write order of the member.</param>
    public BencodePropertyOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the relative write order of the member.
    /// </summary>
    /// <returns>The order value; members are written in ascending order.</returns>
    public int Order { get; }
}
