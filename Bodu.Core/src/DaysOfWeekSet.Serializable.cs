// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Serializable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Runtime.Serialization;

namespace Bodu;

public partial struct DaysOfWeekSet :
    System.Runtime.Serialization.ISerializable
{
    /// <summary>
    /// Initialises a new instance of the <see cref="DaysOfWeekSet" /> structure from serialised data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo" /> containing the serialised data. Must not be <see langword="null" />.</param>
    /// <param name="context">The streaming context for this deserialisation (not used).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="info" /> is <see langword="null" />.</exception>
    /// <exception cref="SerializationException">Thrown if the stored value is outside the valid range 0–127.</exception>
    /// <remarks>
    /// <para>
    /// This constructor supports the <see cref="ISerializable" /> contract and is intended for use with
    /// <c>BinaryFormatter</c>-based deserialisation.
    /// </para>
    /// <para>
    /// <c>BinaryFormatter</c> is obsolete and disabled by default in .NET 5 and later. Applications targeting these runtimes
    /// should prefer a supported serialisation mechanism such as <c>System.Text.Json</c>. A <see cref="DaysOfWeekSet" /> can be
    /// losslessly represented as a single <see cref="byte" /> via <see cref="ToByte" /> and reconstructed via
    /// <see cref="FromByte" />.
    /// </para>
    /// </remarks>
    private DaysOfWeekSet(SerializationInfo info, StreamingContext context)
    {
        ThrowHelper.ThrowIfNull(info);

        int data = info.GetByte(nameof(this._selectedDays));
        if (data < MinValue || data > MaxValue)
            throw new SerializationException(string.Format(ResourceStrings.SerializationException_InvalidState, nameof(DaysOfWeekSet)));

        this._selectedDays = (byte)data;
    }

    /// <summary>
    /// Populates a <see cref="SerializationInfo" /> with the data needed to serialise the current <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo" /> to populate. Must not be <see langword="null" />.</param>
    /// <param name="context">The streaming context for this serialisation (not used).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="info" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This method supports the <see cref="ISerializable" /> contract and is intended for use with
    /// <c>BinaryFormatter</c>-based serialisation.
    /// </para>
    /// <para>
    /// <c>BinaryFormatter</c> is obsolete and disabled by default in .NET 5 and later. Applications targeting these runtimes
    /// should prefer a supported serialisation mechanism. See the remarks on the serialisation constructor for guidance.
    /// </para>
    /// </remarks>
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ThrowHelper.ThrowIfNull(info);

        info.AddValue(nameof(this._selectedDays), this._selectedDays);
    }
}
