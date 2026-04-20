// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an immutable set of parameters (polynomial, width, reflection, initial value, final XOR) that describe a specific CRC algorithm.
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

        /// <summary>Gets the <see cref="CrcStandard" /> that defines the <c>CRC-32/ISO-HDLC</c> cyclic redundancy check algorithm standard.</summary>
        /// <value>A <see cref="CrcStandard" /> object whose properties are set to the <c>CRC-32/ISO-HDLC</c> definition.</value>
        /// <remarks>
        /// <para>The <c>CRC-32/ISO-HDLC</c> standard is taken from the <a href="https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iso-hdlc">CRC RevEng catalogue</a>, with the following definition.</para>
        /// <para>
        /// <list type="bullet">
        /// <item><description>Width: <c>32</c></description></item>
        /// <item><description>Polynomial: <c>0x04C11DB7</c></description></item>
        /// <item><description>Initial Value: <c>0xFFFFFFFF</c></description></item>
        /// <item><description>Reflect In: <c>true</c></description></item>
        /// <item><description>Reflect Out: <c>true</c></description></item>
        /// <item><description>XOR Out: <c>0xFFFFFFFF</c></description></item>
        /// </list>
        /// </para>
        /// <para>The <c>CRC-32/ISO-HDLC</c> standard is also known by the aliases <c>CRC-32</c>, <c>CRC-32/ADCCP</c>, <c>CRC-32/V-42</c>, <c>CRC-32/XZ</c>, and <c>PKZIP</c>.</para>
        /// <para>This entry is maintained in <c>CrcStandard.cs</c> rather than the auto-generated <c>CrcStandard.Catalog.cs</c> because it is the default standard used by <see cref="Crc.Crc()" />. The generated catalogue still references it from <see cref="All" /> and exposes the aliases above.</para>
        /// </remarks>
        /// <seealso cref="CrcStandard.CRC32" />
        /// <seealso cref="CrcStandard.CRC32_ADCCP" />
        /// <seealso cref="CrcStandard.CRC32_V42" />
        /// <seealso cref="CrcStandard.CRC32_XZ" />
        /// <seealso cref="CrcStandard.PKZIP" />
        public static readonly CrcStandard CRC32_ISOHDLC = new CrcStandard(
            name: "CRC-32/ISO-HDLC",
            size: 32,
            polynomial: 0x04C11DB7UL,
            initialValue: 0xFFFFFFFFUL,
            reflectIn: true,
            reflectOut: true,
            xOrOut: 0xFFFFFFFFUL);

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
        /// <exception cref="ArgumentException"><paramref name="name" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size" /> is less than <see cref="MinSize" /> or greater than <see cref="MaxSize" />.
        /// </exception>
        public CrcStandard(string name, int size, ulong polynomial, ulong initialValue, bool reflectIn, bool reflectOut, ulong xOrOut)
        {
            ThrowHelper.ThrowIfNullOrEmpty(name);
            ThrowHelper.ThrowIfOutOfRange(size, MinSize, MaxSize);

            this.Name = name;
            this.Size = size;
            this.Polynomial = polynomial;
            this.InitialValue = initialValue;
            this.ReflectIn = reflectIn;
            this.ReflectOut = reflectOut;
            this.XOrOut = xOrOut;
        }

        private CrcStandard(SerializationInfo info, StreamingContext context)
        {
            ThrowHelper.ThrowIfNull(info);

            this.Name = info.GetString(nameof(this.Name))!;
            this.Size = info.GetInt32(nameof(this.Size));
            this.Polynomial = info.GetUInt64(nameof(this.Polynomial));
            this.InitialValue = info.GetUInt64(nameof(this.InitialValue));
            this.ReflectIn = info.GetBoolean(nameof(this.ReflectIn));
            this.ReflectOut = info.GetBoolean(nameof(this.ReflectOut));
            this.XOrOut = info.GetUInt64(nameof(this.XOrOut));
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
        /// <returns>
        /// <see langword="true" /> if <paramref name="other" /> has the same <see cref="Name" /> (ordinal comparison) and the same parameter
        /// set as this instance; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(CrcStandard? other)
            => other is not null &&
               string.Equals(this.Name, other.Name, StringComparison.Ordinal) &&
               this.Size == other.Size &&
               this.Polynomial == other.Polynomial &&
               this.InitialValue == other.InitialValue &&
               this.ReflectIn == other.ReflectIn &&
               this.ReflectOut == other.ReflectOut &&
               this.XOrOut == other.XOrOut;

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is CrcStandard other && this.Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => HashCode.Combine(this.Name, this.Size, this.Polynomial, this.InitialValue, this.ReflectIn, this.ReflectOut, this.XOrOut);

        /// <inheritdoc />
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ThrowHelper.ThrowIfNull(info);

            info.AddValue(nameof(this.Name), this.Name);
            info.AddValue(nameof(this.Size), this.Size);
            info.AddValue(nameof(this.Polynomial), this.Polynomial);
            info.AddValue(nameof(this.InitialValue), this.InitialValue);
            info.AddValue(nameof(this.ReflectIn), this.ReflectIn);
            info.AddValue(nameof(this.ReflectOut), this.ReflectOut);
            info.AddValue(nameof(this.XOrOut), this.XOrOut);
        }
    }
}