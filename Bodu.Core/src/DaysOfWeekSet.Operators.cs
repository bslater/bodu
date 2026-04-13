// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Operators.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

namespace Bodu;

public partial struct DaysOfWeekSet
{
    /// <summary>
    /// Implicitly converts a <see cref="DaysOfWeekSet" /> to a <see cref="byte" />.
    /// </summary>
    /// <param name="obj">The <see cref="DaysOfWeekSet" /> to convert.</param>
    /// <returns>A <see cref="byte" /> representing the selected-day bitmask.</returns>
    public static implicit operator byte(DaysOfWeekSet obj) =>
        obj._selectedDays;

    /// <summary>
    /// Implicitly converts a <see cref="DaysOfWeekSet" /> to an <see cref="int" />.
    /// </summary>
    /// <param name="obj">The <see cref="DaysOfWeekSet" /> to convert.</param>
    /// <returns>An <see cref="int" /> representing the selected-day bitmask.</returns>
    public static implicit operator int(DaysOfWeekSet obj) =>
        obj._selectedDays;

    /// <summary>
    /// Implicitly converts a <see cref="DaysOfWeekSet" /> to a <see cref="short" />.
    /// </summary>
    /// <param name="obj">The <see cref="DaysOfWeekSet" /> to convert.</param>
    /// <returns>A <see cref="short" /> representing the selected-day bitmask.</returns>
    public static implicit operator short(DaysOfWeekSet obj) =>
        obj._selectedDays;

    /// <summary>
    /// Determines whether two <see cref="DaysOfWeekSet" /> instances have different selected days.
    /// </summary>
    /// <param name="obj1">The first instance.</param>
    /// <param name="obj2">The second instance.</param>
    /// <returns><see langword="true" /> if the instances differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(DaysOfWeekSet obj1, DaysOfWeekSet obj2) =>
        obj1._selectedDays != obj2._selectedDays;

    /// <summary>
    /// Determines whether two <see cref="DaysOfWeekSet" /> instances have the same selected days.
    /// </summary>
    /// <param name="obj1">The first instance.</param>
    /// <param name="obj2">The second instance.</param>
    /// <returns><see langword="true" /> if both instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(DaysOfWeekSet obj1, DaysOfWeekSet obj2) =>
        obj1._selectedDays == obj2._selectedDays;

    /// <summary>
    /// Determines whether one <see cref="DaysOfWeekSet" /> is less than another, based on the underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <(DaysOfWeekSet left, DaysOfWeekSet right) =>
        left._selectedDays < right._selectedDays;

    /// <summary>
    /// Determines whether one <see cref="DaysOfWeekSet" /> is less than or equal to another, based on the underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(DaysOfWeekSet left, DaysOfWeekSet right) =>
        left._selectedDays <= right._selectedDays;

    /// <summary>
    /// Determines whether one <see cref="DaysOfWeekSet" /> is greater than another, based on the underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >(DaysOfWeekSet left, DaysOfWeekSet right) =>
        left._selectedDays > right._selectedDays;

    /// <summary>
    /// Determines whether one <see cref="DaysOfWeekSet" /> is greater than or equal to another, based on the underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(DaysOfWeekSet left, DaysOfWeekSet right) =>
        left._selectedDays >= right._selectedDays;

    /// <summary>
    /// Performs a bitwise AND operation between two <see cref="DaysOfWeekSet" /> instances, returning only the days selected in both.
    /// </summary>
    /// <param name="obj1">The first operand.</param>
    /// <param name="obj2">The second operand.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> containing the intersection of selected days.</returns>
    public static DaysOfWeekSet operator &(DaysOfWeekSet obj1, DaysOfWeekSet obj2) =>
        new DaysOfWeekSet(obj1._selectedDays & obj2._selectedDays);

    /// <summary>
    /// Performs a bitwise XOR operation between two <see cref="DaysOfWeekSet" /> instances, returning the days selected in exactly one.
    /// </summary>
    /// <param name="obj1">The first operand.</param>
    /// <param name="obj2">The second operand.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> containing the symmetric difference of selected days.</returns>
    public static DaysOfWeekSet operator ^(DaysOfWeekSet obj1, DaysOfWeekSet obj2) =>
        new DaysOfWeekSet(obj1._selectedDays ^ obj2._selectedDays);

    /// <summary>
    /// Computes the bitwise complement of a <see cref="DaysOfWeekSet" />, toggling the selection of every day.
    /// </summary>
    /// <param name="obj">The <see cref="DaysOfWeekSet" /> to complement.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> with every day's selection inverted.</returns>
    public static DaysOfWeekSet operator ~(DaysOfWeekSet obj) =>
        new DaysOfWeekSet(~obj._selectedDays & MaxValue);

    /// <summary>
    /// Performs a bitwise OR operation between two <see cref="DaysOfWeekSet" /> instances, returning all days selected in either.
    /// </summary>
    /// <param name="obj1">The first operand.</param>
    /// <param name="obj2">The second operand.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> containing the union of selected days.</returns>
    public static DaysOfWeekSet operator |(DaysOfWeekSet obj1, DaysOfWeekSet obj2) =>
        new DaysOfWeekSet(obj1._selectedDays | obj2._selectedDays);

    /// <summary>
    /// Returns the selected-day bitmask as a <see cref="byte" />.
    /// </summary>
    /// <returns>A <see cref="byte" /> representing the selected days.</returns>
    public byte ToByte() => this._selectedDays;

    /// <summary>
    /// Returns the selected-day bitmask as a <see cref="short" />.
    /// </summary>
    /// <returns>A <see cref="short" /> representing the selected days.</returns>
    public short ToInt16() => (short)this._selectedDays;

    /// <summary>
    /// Returns the selected-day bitmask as an <see cref="int" />.
    /// </summary>
    /// <returns>An <see cref="int" /> representing the selected days.</returns>
    public int ToInt32() => this._selectedDays;
}
