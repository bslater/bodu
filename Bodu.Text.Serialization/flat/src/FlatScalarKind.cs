// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatScalarKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Identifies the scalar a line-format serializer converts a value of a type as, for
/// <see cref="InvariantScalarCodec" />.
/// </summary>
/// <remarks>
/// A type with a kind other than <see cref="None" /> maps to one field, key or value rather than to members, and its
/// <see cref="Nullable{T}" /> form has the same kind. The set is the one <see cref="InvariantScalarCodec.Classify" />
/// lists, so the binders' scalar test, the codec's parsing and the source generator's parity test share it.
/// </remarks>
internal enum FlatScalarKind
{
    /// <summary>
    /// The type is not a scalar.
    /// </summary>
    None = 0,

    /// <summary>
    /// A <see cref="string" />, kept as written.
    /// </summary>
    String,

    /// <summary>
    /// A <see cref="bool" />, written <c>true</c> or <c>false</c>.
    /// </summary>
    Boolean,

    /// <summary>
    /// A <see cref="char" />.
    /// </summary>
    Char,

    /// <summary>
    /// A number <see cref="IConvertible" /> converts: <see cref="sbyte" />, <see cref="byte" />, <see cref="short" />,
    /// <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />,
    /// <see cref="float" />, <see cref="double" /> or <see cref="decimal" />.
    /// </summary>
    ConvertibleNumber,

    /// <summary>
    /// A native-sized signed integer, <see cref="nint" />.
    /// </summary>
    NativeInteger,

    /// <summary>
    /// A native-sized unsigned integer, <see cref="nuint" />.
    /// </summary>
    NativeUnsignedInteger,

    /// <summary>
    /// An <see cref="System.Int128" />.
    /// </summary>
    Int128,

    /// <summary>
    /// A <see cref="System.UInt128" />.
    /// </summary>
    UInt128,

    /// <summary>
    /// A <see cref="System.Half" />.
    /// </summary>
    Half,

    /// <summary>
    /// A <see cref="System.Numerics.BigInteger" />.
    /// </summary>
    BigInteger,

    /// <summary>
    /// An enumeration, read by name or number, ignoring case.
    /// </summary>
    Enum,

    /// <summary>
    /// A <see cref="System.Guid" />.
    /// </summary>
    Guid,

    /// <summary>
    /// A <see cref="System.DateTime" />.
    /// </summary>
    DateTime,

    /// <summary>
    /// A <see cref="System.DateTimeOffset" />.
    /// </summary>
    DateTimeOffset,

    /// <summary>
    /// A <see cref="System.DateOnly" />.
    /// </summary>
    DateOnly,

    /// <summary>
    /// A <see cref="System.TimeOnly" />.
    /// </summary>
    TimeOnly,

    /// <summary>
    /// A <see cref="System.TimeSpan" />.
    /// </summary>
    TimeSpan,

    /// <summary>
    /// A <see cref="System.Uri" />, relative or absolute.
    /// </summary>
    Uri,
}
