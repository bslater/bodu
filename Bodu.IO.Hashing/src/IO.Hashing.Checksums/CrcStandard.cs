// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable SYSLIB0050 // CrcStandard intentionally implements ISerializable for cross-process serialization of CRC parameters.

using System.Runtime.Serialization;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Immutable parameter bundle that fully describes a CRC variant — width, polynomial, initial value, input/output bit
/// reflection, and final XOR — and serves as the lookup key into the RevEng catalogue used by <see cref="Crc"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every CRC variant in the wild differs from every other along the same five dimensions — the polynomial, the
/// initial register value, whether input bits are reflected as they enter the register, whether the final register is
/// reflected on the way out, and what value the register is XOR-ed with at the end. <c>CRC-32/ISO-HDLC</c> and
/// <c>CRC-32/BZIP2</c> share a polynomial but disagree on reflection; <c>CRC-16/MODBUS</c> and <c>CRC-16/USB</c> share
/// nearly everything but the initial value. <see cref="CrcStandard"/> captures one such parameter bundle as an
/// immutable, equatable, serializable value, decoupling the choice of variant from the engine that runs it.
/// </para>
/// <para>
/// <strong>Three ways to obtain a standard.</strong>
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Catalogue properties</term>
///     <description><see cref="CRC32_ISOHDLC"/>, <see cref="CRC32_ISCSI"/>, <see cref="CRC16_MODBUS"/>,
///     <see cref="CRC16_KERMIT"/>, <see cref="CRC64_XZ"/>, … — direct named accessors for the common entries that
///     return the canonical, cached <see cref="CrcStandard"/> instance.</description>
///   </item>
///   <item>
///     <term><see cref="Get(CrcStandards)"/></term>
///     <description>Materializes any catalogue entry from its <see cref="CrcStandards"/> enum value — useful when the
///     choice is data-driven (e.g. read from configuration).</description>
///   </item>
///   <item>
///     <term><see cref="FromName(string)"/></term>
///     <description>Resolves canonical names <em>and</em> published aliases — <c>"CRC-32"</c>, <c>"PKZIP"</c>,
///     <c>"CRC-32/XZ"</c>, <c>"CRC-32/ADCCP"</c> all return the same <see cref="CRC32_ISOHDLC"/> instance.</description>
///   </item>
///   <item>
///     <term>Direct construction</term>
///     <description>The
///     <see cref="CrcStandard(string, int, ulong, ulong, bool, bool, ulong)"/> constructor accepts custom parameters
///     for variants that are not in the RevEng catalogue. Width must lie within
///     <see cref="MinSize"/>..<see cref="MaxSize"/>.</description>
///   </item>
/// </list>
/// <para>
/// <strong>How it pairs with <see cref="Crc"/>.</strong> The hash engine reads <see cref="Polynomial"/> and
/// <see cref="ReflectIn"/> to fetch (or build) a precomputed lookup table from
/// <see cref="CrcLookupTableCache"/>, seeds the running register with <see cref="InitialValue"/>, and applies
/// <see cref="ReflectOut"/> + <see cref="XOrOut"/> when emitting the final digest. Multiple <see cref="Crc"/>
/// instances configured with the same <see cref="CrcStandard"/> share the same lookup table, so creating fresh
/// engines for the same standard is inexpensive.
/// </para>
/// <para>
/// <strong>Equality and serialization.</strong> <see cref="CrcStandard"/> is a value-style type: two instances are
/// equal iff every parameter matches (the <see cref="Name"/> is informational only and does not affect identity).
/// <see cref="System.Runtime.Serialization.ISerializable"/> support is provided so a chosen standard can survive
/// process boundaries unchanged.
/// </para>
/// <para>
/// <strong>Coverage and limits.</strong> Sizes are constrained to <see cref="MinSize"/>..<see cref="MaxSize"/> bits;
/// any wider variant in the RevEng catalogue (currently only <c>CRC-82/DARC</c>) is intentionally omitted because it
/// will not fit in the <see cref="ulong"/>-backed register. Instances are immutable and therefore safe to share
/// across threads.
/// </para>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing.Checksums;
///
/// // 1. Direct named accessor — most callers want exactly this.
/// var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
///
/// // 2. Data-driven look-up — pick the variant from a configuration value.
/// CrcStandards configured = Enum.Parse&lt;CrcStandards&gt;(config["CrcVariant"]);
/// var configuredCrc = new Crc(CrcStandard.Get(configured));
///
/// // 3. Look-up by alias — accept legacy or vendor names supplied by users.
/// CrcStandard pkzip = CrcStandard.FromName("PKZIP");          // same instance as CRC32_ISOHDLC
/// CrcStandard ccitt = CrcStandard.FromName("CRC-CCITT");      // resolves to CRC16_KERMIT
///
/// // 4. Custom variant (not in the RevEng catalogue): a 12-bit CRC with bespoke parameters.
/// var custom = new CrcStandard(
///     name: "CRC-12/MY-SPEC",
///     size: 12,
///     polynomial:   0x80F,
///     initialValue: 0x000,
///     reflectIn: false, reflectOut: false,
///     xOrOut: 0x000);
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Crc"/>
/// <seealso cref="CrcStandards"/>
/// <seealso cref="CrcLookupTableCache"/>
[Serializable]
public sealed partial class CrcStandard
    : System.Runtime.Serialization.ISerializable,
      System.IEquatable<CrcStandard>
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

        var serializedName = info.GetString(nameof(this.Name));
        this.Name = serializedName!;
        this.Size = info.GetInt32(nameof(this.Size));
        this.Polynomial = info.GetUInt64(nameof(this.Polynomial));
        this.InitialValue = info.GetUInt64(nameof(this.InitialValue));
        this.ReflectIn = info.GetBoolean(nameof(this.ReflectIn));
        this.ReflectOut = info.GetBoolean(nameof(this.ReflectOut));
        this.XOrOut = info.GetUInt64(nameof(this.XOrOut));
    }

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

    /// <summary>Gets the <c>CRC-32/BZIP2</c> CRC standard (aliases <c>CRC-32/AAL5</c>, <c>CRC-32/DECT-B</c>, <c>B-CRC-32</c>). Used by bzip2 and AAL5.</summary>
    /// <seealso cref="CrcStandards.CRC32_BZIP2" />
    public static CrcStandard CRC32_BZIP2 => Get(CrcStandards.CRC32_BZIP2);

    /// <summary>Gets the <c>CRC-32/ISCSI</c> CRC standard (aliases <c>CRC-32C</c>, <c>CRC-32/CASTAGNOLI</c>, <c>CRC-32/NVME</c>, <c>CRC-32/BASE91-C</c>, <c>CRC-32/INTERLAKEN</c>). Castagnoli polynomial used by iSCSI, SCTP, Btrfs, ext4, NVMe, and many modern protocols.</summary>
    /// <seealso cref="CrcStandards.CRC32_ISCSI" />
    public static CrcStandard CRC32_ISCSI => Get(CrcStandards.CRC32_ISCSI);

    /// <summary>Gets the <c>CRC-32/ISO-HDLC</c> CRC standard (aliases <c>CRC-32</c>, <c>PKZIP</c>, <c>CRC-32/XZ</c>, <c>CRC-32/ADCCP</c>, <c>CRC-32/V-42</c>). The canonical CRC-32 used by zlib, PNG, Ethernet, PKZIP, and the default standard used by <see cref="Crc.Crc()" />.</summary>
    /// <remarks>
    /// <para>Width 32, polynomial <c>0x04C11DB7</c>, initial value <c>0xFFFFFFFF</c>, reflected input and output, final XOR <c>0xFFFFFFFF</c>.</para>
    /// </remarks>
    /// <seealso cref="CrcStandards.CRC32_ISOHDLC" />
    public static CrcStandard CRC32_ISOHDLC => Get(CrcStandards.CRC32_ISOHDLC);

    /// <summary>Gets the <c>CRC-64/ECMA-182</c> CRC standard (alias <c>CRC-64</c>). Specified by ECMA-182 for DLT tape formats.</summary>
    /// <seealso cref="CrcStandards.CRC64_ECMA182" />
    public static CrcStandard CRC64_ECMA182 => Get(CrcStandards.CRC64_ECMA182);

    /// <summary>Gets the <c>CRC-64/XZ</c> CRC standard (alias <c>CRC-64/GO-ECMA</c>). Used by the XZ compressed file format and the Go <c>crc64</c> ECMA table.</summary>
    /// <seealso cref="CrcStandards.CRC64_XZ" />
    public static CrcStandard CRC64_XZ => Get(CrcStandards.CRC64_XZ);

    /// <summary>Gets the <c>CRC-8/MAXIM-DOW</c> CRC standard (aliases <c>CRC-8/MAXIM</c>, <c>DOW-CRC</c>). Used by Maxim / Dallas 1-Wire devices.</summary>
    /// <seealso cref="CrcStandards.CRC8_MAXIMDOW" />
    public static CrcStandard CRC8_MAXIMDOW => Get(CrcStandards.CRC8_MAXIMDOW);

    /// <summary>Gets the <c>CRC-8/SMBUS</c> CRC standard (alias <c>CRC-8</c>). Width 8, polynomial <c>0x07</c>, initial value <c>0x00</c>, no reflection, XOR out <c>0x00</c>.</summary>
    /// <remarks>The canonical "CRC-8" used by SMBus (System Management Bus) and the de-facto generic 8-bit CRC.</remarks>
    /// <seealso cref="CrcStandards.CRC8_SMBUS" />
    public static CrcStandard CRC8_SMBUS => Get(CrcStandards.CRC8_SMBUS);

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

}
