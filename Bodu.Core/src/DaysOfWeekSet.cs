// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents a set of selected days in a standard seven-day week.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DaysOfWeekSet" /> provides a compact bitmask representation of selected days, supporting parsing, formatting, comparison,
/// and serialization.
/// </para>
/// <code language="csharp">
///<![CDATA[
/// Create a set with Monday, Wednesday, and Friday selected
/// var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
///
/// Check if Friday is selected:
/// bool isFridayIncluded = days[DayOfWeek.Friday]; // true
///
/// Add Sunday to the set
/// days[DayOfWeek.Sunday] = true;
///
/// Count the number of selected days
/// int selectedCount = days.Count; //  4
///]]>
/// </code>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public partial struct DaysOfWeekSet
{
#pragma warning disable IDE1006

    /// <summary>
    /// Represents a <see cref="DaysOfWeekSet" /> with no selected days.
    /// </summary>
    public static readonly DaysOfWeekSet Empty;

    /// <summary>
    /// Represents a <see cref="DaysOfWeekSet" /> with Monday to Friday selected.
    /// </summary>
    public static readonly DaysOfWeekSet Weekdays = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Represents a <see cref="DaysOfWeekSet" /> with Saturday and Sunday selected.
    /// </summary>
    public static readonly DaysOfWeekSet Weekend = new DaysOfWeekSet(DayOfWeek.Saturday, DayOfWeek.Sunday);

#pragma warning restore IDE1006

    private const int MaxValue = 0b1111111;
    private const int MinValue = 0b0000000;
    private const byte ShiftValue = 0x01;

#pragma warning disable IDE1006
    private static readonly char[] WeekdaySymbols = { 'S', 'M', 'T', 'W', 'T', 'F', 'S' };
#pragma warning restore IDE1006

    private byte _selectedDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaysOfWeekSet" /> structure with the specified selected days.
    /// </summary>
    /// <param name="daysOfWeek">An array of <see cref="DayOfWeek" /> values to mark as selected.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any provided day is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Create a set with Monday, Wednesday, and Friday marked as selected
    /// var workdays = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
    ///
    /// Check if a specific day is selected
    /// bool isFridaySelected = workdays[DayOfWeek.Friday]; // true
    ///
    /// Get the total number of selected days
    /// int count = workdays.Count; // 3
    ///]]>
    /// </code>
    /// </example>
    public DaysOfWeekSet(params DayOfWeek[] daysOfWeek)
    {
        _selectedDays = 0;
        foreach (DayOfWeek day in daysOfWeek ?? Array.Empty<DayOfWeek>())
            this[day] = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DaysOfWeekSet" /> struct by parsing the specified string.
    /// </summary>
    /// <param name="input">The input string representing selected days.</param>
    /// <exception cref="FormatException">
    /// Thrown if the <paramref name="input" /> is <see langword="null" />, not exactly 7 characters long, or contains invalid characters.
    /// </exception>
    /// <remarks>
    /// This constructor behaves the same as <see cref="Parse(string)" />, automatically inferring the format from the input string.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Parse a string where 'M', 'T', 'W', 'T', 'F' represent selected days, '-' means unselected
    /// var weekdays = new DaysOfWeekSet("MTWTF--");
    ///
    /// Check if Thursday is selected
    /// bool thursdayIncluded = weekdays[DayOfWeek.Thursday]; // true
    ///
    /// Check if Sunday is selected
    /// bool sundayIncluded = weekdays[DayOfWeek.Sunday]; // false
    ///
    /// Count the number of selected days
    /// int count = weekdays.Count; // 5
    ///]]>
    /// </code>
    /// </example>
    public DaysOfWeekSet(string input)
        : this(Parse(input)) // delegates to the implicit copy constructor from static method
    { }

    private DaysOfWeekSet(int value)
    {
        ThrowHelper.ThrowIfOutOfRange(value, MinValue, MaxValue);

        _selectedDays = (byte)value;
    }

    /// <summary>
    /// Gets the number of days currently selected.
    /// </summary>
    /// <value>An <see cref="int" /> representing the total number of selected days.</value>
    public int Count =>
        BitOperations.PopCount(_selectedDays);

    /// <summary>
    /// Gets or sets whether the specified <see cref="DayOfWeek" /> is selected.
    /// </summary>
    /// <param name="dayOfWeek">The day to get or set.</param>
    /// <returns><c>true</c> if selected; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek" /> is not a valid day (0–6).</exception>
    public bool this[DayOfWeek dayOfWeek]
    {
        get
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

            return this[(int)dayOfWeek];
        }

        set
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

            this[(int)dayOfWeek] = value;
        }
    }

    private bool this[int dayOfWeek]
    {
        get => (_selectedDays >> (6 - dayOfWeek) & 1) == 1;

        set
        {
            int realBit = 6 - dayOfWeek;
            byte mask = (byte)(ShiftValue << realBit);
            _selectedDays = value
                ? (byte)(_selectedDays | mask)
                : (byte)(_selectedDays & ~mask);
        }
    }

    /// <summary>
    /// Creates a <see cref="DaysOfWeekSet" /> from a raw <see cref="byte" /> value representing selected days.
    /// </summary>
    /// <param name="value">The byte value to interpret as selected days.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value exceeds the valid mask (0–127).</exception>
    public static DaysOfWeekSet FromByte(byte value) => value <= MaxValue ? new DaysOfWeekSet(value) : throw new ArgumentOutOfRangeException(nameof(value));

    /// <summary>
    /// Clears all selected days.
    /// </summary>
    public void Clear() => _selectedDays = 0;

    /// <summary>
    /// Determines whether the specified <see cref="object" /> is equal to the current <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is DaysOfWeekSet other && _selectedDays == other._selectedDays;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code representing the current <see cref="DaysOfWeekSet" />.</returns>
    public override int GetHashCode() => _selectedDays.GetHashCode();
}
