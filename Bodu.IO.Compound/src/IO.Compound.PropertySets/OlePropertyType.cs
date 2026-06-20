// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertyType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Identifies the scalar value type carried by an <see cref="OlePropertyValue" />, corresponding to the COM
/// <c>VARENUM</c> (<c>VT_*</c>) constants used in OLE property sets.
/// </summary>
/// <remarks>
/// These values are the base (scalar) type only; the vector modifier (<c>VT_VECTOR</c>) is surfaced separately through
/// <see cref="OlePropertyValue.IsVector" />.
/// </remarks>
public enum OlePropertyType : ushort
{
    /// <summary>
    /// An empty value (<c>VT_EMPTY</c>).
    /// </summary>
    Empty = 0,

    /// <summary>
    /// A null value (<c>VT_NULL</c>).
    /// </summary>
    Null = 1,

    /// <summary>
    /// A signed 16-bit integer (<c>VT_I2</c>).
    /// </summary>
    Int16 = 2,

    /// <summary>
    /// A signed 32-bit integer (<c>VT_I4</c>).
    /// </summary>
    Int32 = 3,

    /// <summary>
    /// A 32-bit IEEE floating-point value (<c>VT_R4</c>).
    /// </summary>
    Float32 = 4,

    /// <summary>
    /// A 64-bit IEEE floating-point value (<c>VT_R8</c>).
    /// </summary>
    Float64 = 5,

    /// <summary>
    /// A currency value (<c>VT_CY</c>), surfaced as <see cref="decimal" />.
    /// </summary>
    Currency = 6,

    /// <summary>
    /// An OLE automation date (<c>VT_DATE</c>), surfaced as <see cref="DateTimeOffset" />.
    /// </summary>
    Date = 7,

    /// <summary>
    /// A length-prefixed code-page string (<c>VT_BSTR</c>).
    /// </summary>
    BinaryString = 8,

    /// <summary>
    /// A boolean value (<c>VT_BOOL</c>).
    /// </summary>
    Boolean = 11,

    /// <summary>
    /// A signed 8-bit integer (<c>VT_I1</c>).
    /// </summary>
    Int8 = 16,

    /// <summary>
    /// An unsigned 8-bit integer (<c>VT_UI1</c>).
    /// </summary>
    UInt8 = 17,

    /// <summary>
    /// An unsigned 16-bit integer (<c>VT_UI2</c>).
    /// </summary>
    UInt16 = 18,

    /// <summary>
    /// An unsigned 32-bit integer (<c>VT_UI4</c>).
    /// </summary>
    UInt32 = 19,

    /// <summary>
    /// A signed 64-bit integer (<c>VT_I8</c>).
    /// </summary>
    Int64 = 20,

    /// <summary>
    /// An unsigned 64-bit integer (<c>VT_UI8</c>).
    /// </summary>
    UInt64 = 21,

    /// <summary>
    /// A length-prefixed ANSI string decoded with the section code page (<c>VT_LPSTR</c>).
    /// </summary>
    AnsiString = 30,

    /// <summary>
    /// A length-prefixed UTF-16 string (<c>VT_LPWSTR</c>).
    /// </summary>
    UnicodeString = 31,

    /// <summary>
    /// A Windows FILETIME (<c>VT_FILETIME</c>), surfaced as <see cref="DateTimeOffset" /> or <see cref="TimeSpan" />.
    /// </summary>
    FileTime = 64,

    /// <summary>
    /// An opaque length-prefixed binary blob (<c>VT_BLOB</c>).
    /// </summary>
    Blob = 65,

    /// <summary>
    /// Clipboard data (<c>VT_CF</c>), surfaced as a byte array.
    /// </summary>
    ClipboardData = 71,
}
