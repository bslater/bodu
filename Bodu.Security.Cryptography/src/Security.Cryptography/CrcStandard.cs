// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Drawing;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Represents the configuration settings for a CRC algorithm, including parameters like polynomial, initial value, reflection settings,
    /// and more.
    /// </summary>
    [Serializable]
    public sealed partial class CrcStandard
        : System.Runtime.Serialization.ISerializable
        , System.IEquatable<CrcStandard>
    {
        /// <summary>
        /// The maximum size allowed for a CRC standard (in bits).
        /// </summary>
        public const int MaxSize = 64;

        /// <summary>
        /// The minimum size allowed for a CRC standard (in bits).
        /// </summary>
        public const int MinSize = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrcStandard" /> class with the specified parameters.
        /// </summary>
        /// <param name="name">The name of the CRC standard.</param>
        /// <param name="size">The size, in bits, of the CRC checksum.</param>
        /// <param name="polynomial">The CRC polynomial value.</param>
        /// <param name="initialValue">The initial value used for the CRC calculation.</param>
        /// <param name="reflectIn">Indicates whether to reflect the input during the CRC calculation.</param>
        /// <param name="reflectOut">Indicates whether to reflect the output during the CRC calculation.</param>
        /// <param name="xOrOut">The value to XOR the final output with.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="name" /><see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="size" /> is outside the valid range.</exception>
        public CrcStandard(string name, int size, ulong polynomial, ulong initialValue, bool reflectIn, bool reflectOut, ulong xOrOut)
        {
            ThrowHelper.ThrowIfNullOrEmpty(name);
            ThrowHelper.ThrowIfOutOfRange(size, MinSize, MaxSize);

            Name = name;
            Size = size;
            Polynomial = polynomial;
            InitialValue = initialValue;
            ReflectIn = reflectIn;
            ReflectOut = reflectOut;
            XOrOut = xOrOut;
        }

        private CrcStandard(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            Name = info.GetString(nameof(Name))!;
            Size = info.GetInt32(nameof(Size));
            Polynomial = info.GetUInt64(nameof(Polynomial));
            InitialValue = info.GetUInt64(nameof(InitialValue));
            ReflectIn = info.GetBoolean(nameof(ReflectIn));
            ReflectOut = info.GetBoolean(nameof(ReflectOut));
            XOrOut = info.GetUInt64(nameof(XOrOut));
        }

        /// <summary>
        /// Gets the initial value used in the CRC calculation.
        /// </summary>
        /// <value>The initial value for the CRC calculation.</value>
        public ulong InitialValue { get; init; }

        /// <summary>
        /// Gets the name of the CRC standard.
        /// </summary>
        /// <value>The name of the CRC algorithm.</value>
        public string Name { get; init; }

        /// <summary>
        /// Gets the polynomial used in the CRC calculation.
        /// </summary>
        /// <value>The polynomial value used in the CRC calculation.</value>
        public ulong Polynomial { get; init; }

        /// <summary>
        /// Gets a value indicating whether the input data is reflected during the CRC calculation.
        /// </summary>
        /// <value><see langword="true" /> if input data is reflected; otherwise, <see langword="false" />.</value>
        public bool ReflectIn { get; init; }

        /// <summary>
        /// Gets a value indicating whether the CRC result is reflected before XORing with <see cref="XOrOut" />.
        /// </summary>
        /// <value><see langword="true" /> if the result is reflected; otherwise, <see langword="false" />.</value>
        public bool ReflectOut { get; init; }

        /// <summary>
        /// Gets the size, in bits, of the CRC checksum.
        /// </summary>
        /// <value>The size of the CRC in bits.</value>
        public int Size { get; init; }

        /// <summary>
        /// Gets the value to XOR the final CRC result with.
        /// </summary>
        /// <value>The XOR value for the final CRC result.</value>
        public ulong XOrOut { get; init; }

        /// <summary>
        /// Determines whether the current <see cref="CrcStandard" /> object is equal to another <see cref="CrcStandard" /> object.
        /// </summary>
        /// <param name="other">The other <see cref="CrcStandard" /> object to compare.</param>
        /// <returns><see langword="true" /> if the two objects are equal; otherwise, <see langword="false" />.</returns>
        public bool Equals(CrcStandard? other)
            => other is not null &&
               Size == other.Size &&
               Polynomial == other.Polynomial &&
               InitialValue == other.InitialValue &&
               ReflectIn == other.ReflectIn &&
               ReflectOut == other.ReflectOut &&
               XOrOut == other.XOrOut;

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is CrcStandard other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => HashCode.Combine(Size, Polynomial, InitialValue, ReflectIn, ReflectOut, XOrOut);

        /// <inheritdoc />
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Size), Size);
            info.AddValue(nameof(Polynomial), Polynomial);
            info.AddValue(nameof(InitialValue), InitialValue);
            info.AddValue(nameof(ReflectIn), ReflectIn);
            info.AddValue(nameof(ReflectOut), ReflectOut);
            info.AddValue(nameof(XOrOut), XOrOut);
        }
    }
}