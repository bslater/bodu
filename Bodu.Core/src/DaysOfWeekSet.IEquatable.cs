// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.IEquatable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

namespace Bodu;

public partial struct DaysOfWeekSet :
    System.IEquatable<DaysOfWeekSet>,
    System.IEquatable<byte>
{
    /// <summary>
    /// Determines whether the specified <see cref="DaysOfWeekSet" /> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="DaysOfWeekSet" /> to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> has the same set of selected days; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DaysOfWeekSet other) => this._selectedDays == other._selectedDays;

    /// <summary>
    /// Determines whether the specified <see cref="byte" /> value is equal to the internal bitmask of the current instance.
    /// </summary>
    /// <param name="other">The <see cref="byte" /> value to compare with the internal bitmask of this instance.</param>
    /// <returns>
    /// <see langword="true" /> if the bit pattern of <paramref name="other" /> matches the selected days; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(byte other) => this._selectedDays == other;
}
