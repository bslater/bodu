// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Serializable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Bodu;

public partial struct WeekPattern : System.Runtime.Serialization.ISerializable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeekPattern" /> struct from the given serialized data.
    /// </summary>
    /// <param name="info">
    /// The <see cref="SerializationInfo" /> containing the serialized data. Must not be <see langword="null" />.
    /// </param>
    /// <param name="context">The streaming context (not used).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="info" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="SerializationException">
    /// Thrown if the stored bitmask value is outside the valid range [0, 127].
    /// </exception>
    private WeekPattern(SerializationInfo info, StreamingContext context)
    {
        ThrowHelper.ThrowIfNull(info);

        int data = info.GetByte(nameof(_selectedDays));
        if (data is < MinValue or > MaxValue) throw new SerializationException(string.Format(ResourceStrings.Serial_Invalid_State, nameof(WeekPattern)));

        _selectedDays = (byte)data;
    }

    /// <summary>
    /// Populates a <see cref="SerializationInfo" /> with the data required to serialize the current
    /// <see cref="WeekPattern" />.
    /// </summary>
    /// <param name="info">
    /// The <see cref="SerializationInfo" /> to populate. Must not be <see langword="null" />.
    /// </param>
    /// <param name="context">The streaming context (not used).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="info" /> is <see langword="null" />.
    /// </exception>
    readonly void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ThrowHelper.ThrowIfNull(info);
        info.AddValue(nameof(_selectedDays), _selectedDays);
    }
}
