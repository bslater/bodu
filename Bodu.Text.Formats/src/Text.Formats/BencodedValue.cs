// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a value in the Bencode serialization format.
/// </summary>
public abstract class BencodedValue
{

    /// <summary>
    /// Gets the concrete kind of the bencoded value.
    /// </summary>
    public abstract BencodedValueKind Kind { get; }

}
