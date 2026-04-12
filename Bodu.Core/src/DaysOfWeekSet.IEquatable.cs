// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSet.IEquatable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct DaysOfWeekSet :
    System.IEquatable<DaysOfWeekSet>,
    System.IEquatable<byte>
{
    /// <summary>
    /// Determines whether the specified <see cref="DaysOfWeekSet" /> is equal to the current <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="other">The <see cref="DaysOfWeekSet" /> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="DaysOfWeekSet" /> has the same selected days; otherwise, <c>false</c>.</returns>
    public bool Equals(DaysOfWeekSet other) => _selectedDays == other._selectedDays;

    /// <summary>
    /// Determines whether the specified <see cref="byte" /> value is equal to the current <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="other">The <see cref="byte" /> value to compare with this instance.</param>
    /// <returns><c>true</c> if the bit pattern matches; otherwise, <c>false</c>.</returns>
    public bool Equals(byte other) => _selectedDays == other;
}
