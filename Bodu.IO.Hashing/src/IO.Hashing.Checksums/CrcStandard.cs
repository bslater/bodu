// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Bodu.IO.Hashing.Checksums;

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

    /// <summary>Gets the <c>CRC-8/SMBUS</c> CRC standard (alias <c>CRC-8</c>). Width 8, polynomial <c>0x07</c>, initial value <c>0x00</c>, no reflection, XOR out <c>0x00</c>.</summary>
    /// <remarks>The canonical "CRC-8" used by SMBus (System Management Bus) and the de-facto generic 8-bit CRC.</remarks>
    /// <seealso cref="CrcStandards.CRC8_SMBUS" />
    public static CrcStandard CRC8_SMBUS => Get(CrcStandards.CRC8_SMBUS);

    /// <summary>Gets the <c>CRC-8/MAXIM-DOW</c> CRC standard (aliases <c>CRC-8/MAXIM</c>, <c>DOW-CRC</c>). Used by Maxim / Dallas 1-Wire devices.</summary>
    /// <seealso cref="CrcStandards.CRC8_MAXIMDOW" />
    public static CrcStandard CRC8_MAXIMDOW => Get(CrcStandards.CRC8_MAXIMDOW);

    /// <summary>Gets the <c>CRC-16/ARC</c> CRC standard (aliases include <c>CRC-16</c>, <c>CRC-IBM</c>, <c>CRC-16/LHA</c>). The de-facto generic 16-bit CRC.</summary>
    /// <seealso cref="CrcStandards.CRC16_ARC" />
    public static CrcStandard CRC16_ARC => Get(CrcStandards.CRC16_ARC);

    /// <summary>Gets the <c>CRC-16/IBM-3740</c> CRC standard (aliases <c>CRC-16/AUTOSAR</c>, <c>CRC-16/CCITT-FALSE</c>). The widely-used "CCITT-false" variant.</summary>
    /// <seealso cref="CrcStandards.CRC16_IBM3740" />
    public static CrcStandard CRC16_IBM3740 => Get(CrcStandards.CRC16_IBM3740);

    /// <summary>Gets the <c>CRC-16/KERMIT</c> CRC standard (aliases <c>CRC-16/BLUETOOTH</c>, <c>CRC-16/CCITT</c>, <c>CRC-CCITT</c>). Used by Kermit and Bluetooth.</summary>
    /// <seealso cref="CrcStandards.CRC16_KERMIT" />
    public static CrcStandard CRC16_KERMIT => Get(CrcStandards.CRC16_KERMIT);

    /// <summary>Gets the <c>CRC-16/MODBUS</c> CRC standard (alias <c>MODBUS</c>). Used by Modbus RTU over serial.</summary>
    /// <seealso cref="CrcStandards.CRC16_MODBUS" />
    public static CrcStandard CRC16_MODBUS => Get(CrcStandards.CRC16_MODBUS);

    /// <summary>Gets the <c>CRC-16/XMODEM</c> CRC standard (aliases <c>CRC-16/ACORN</c>, <c>XMODEM</c>, <c>ZMODEM</c>). Used by XMODEM and ZMODEM.</summary>
    /// <seealso cref="CrcStandards.CRC16_XMODEM" />
    public static CrcStandard CRC16_XMODEM => Get(CrcStandards.CRC16_XMODEM);

    /// <summary>Gets the <c>CRC-32/ISO-HDLC</c> CRC standard (aliases <c>CRC-32</c>, <c>PKZIP</c>, <c>CRC-32/XZ</c>, <c>CRC-32/ADCCP</c>, <c>CRC-32/V-42</c>). The canonical CRC-32 used by zlib, PNG, Ethernet, PKZIP, and the default standard used by <see cref="Crc.Crc()" />.</summary>
    /// <remarks>
    /// <para>Width 32, polynomial <c>0x04C11DB7</c>, initial value <c>0xFFFFFFFF</c>, reflected input and output, final XOR <c>0xFFFFFFFF</c>.</para>
    /// </remarks>
    /// <seealso cref="CrcStandards.CRC32_ISOHDLC" />
    public static CrcStandard CRC32_ISOHDLC => Get(CrcStandards.CRC32_ISOHDLC);

    /// <summary>Gets the <c>CRC-32/ISCSI</c> CRC standard (aliases <c>CRC-32C</c>, <c>CRC-32/CASTAGNOLI</c>, <c>CRC-32/NVME</c>, <c>CRC-32/BASE91-C</c>, <c>CRC-32/INTERLAKEN</c>). Castagnoli polynomial used by iSCSI, SCTP, Btrfs, ext4, NVMe, and many modern protocols.</summary>
    /// <seealso cref="CrcStandards.CRC32_ISCSI" />
    public static CrcStandard CRC32_ISCSI => Get(CrcStandards.CRC32_ISCSI);

    /// <summary>Gets the <c>CRC-32/BZIP2</c> CRC standard (aliases <c>CRC-32/AAL5</c>, <c>CRC-32/DECT-B</c>, <c>B-CRC-32</c>). Used by bzip2 and AAL5.</summary>
    /// <seealso cref="CrcStandards.CRC32_BZIP2" />
    public static CrcStandard CRC32_BZIP2 => Get(CrcStandards.CRC32_BZIP2);

    /// <summary>Gets the <c>CRC-64/ECMA-182</c> CRC standard (alias <c>CRC-64</c>). Specified by ECMA-182 for DLT tape formats.</summary>
    /// <seealso cref="CrcStandards.CRC64_ECMA182" />
    public static CrcStandard CRC64_ECMA182 => Get(CrcStandards.CRC64_ECMA182);

    /// <summary>Gets the <c>CRC-64/XZ</c> CRC standard (alias <c>CRC-64/GO-ECMA</c>). Used by the XZ compressed file format and the Go <c>crc64</c> ECMA table.</summary>
    /// <seealso cref="CrcStandards.CRC64_XZ" />
    public static CrcStandard CRC64_XZ => Get(CrcStandards.CRC64_XZ);

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
