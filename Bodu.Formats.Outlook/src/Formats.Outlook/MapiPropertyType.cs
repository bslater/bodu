// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Specifies the 16-bit MAPI property type codes (the <c>PT_*</c> constants) carried in the low word of a
/// <see cref="MapiPropertyTag" />.
/// </summary>
/// <remarks>
/// The values are the base type codes defined by MS-OXCDATA §2.11.1. A multi-valued property combines a base code with
/// the <c>0x1000</c> flag, which <see cref="MapiPropertyTag" /> strips from <see cref="MapiPropertyTag.Type" /> and
/// surfaces separately through <see cref="MapiPropertyTag.IsMultiValued" />.
/// </remarks>
public enum MapiPropertyType : ushort
{
    /// <summary>
    /// The property type is unspecified (<c>PT_UNSPECIFIED</c>).
    /// </summary>
    Unspecified = 0x0000,

    /// <summary>
    /// No value is present (<c>PT_NULL</c>).
    /// </summary>
    Null = 0x0001,

    /// <summary>
    /// A signed 16-bit integer (<c>PT_SHORT</c> / <c>PT_I2</c>).
    /// </summary>
    Int16 = 0x0002,

    /// <summary>
    /// A signed 32-bit integer (<c>PT_LONG</c> / <c>PT_I4</c>).
    /// </summary>
    Int32 = 0x0003,

    /// <summary>
    /// A 32-bit IEEE floating-point value (<c>PT_FLOAT</c> / <c>PT_R4</c>).
    /// </summary>
    Float = 0x0004,

    /// <summary>
    /// A 64-bit IEEE floating-point value (<c>PT_DOUBLE</c> / <c>PT_R8</c>).
    /// </summary>
    Double = 0x0005,

    /// <summary>
    /// A currency amount scaled by 10,000 (<c>PT_CURRENCY</c>).
    /// </summary>
    Currency = 0x0006,

    /// <summary>
    /// An OLE automation date (<c>PT_APPTIME</c>).
    /// </summary>
    AppTime = 0x0007,

    /// <summary>
    /// A 32-bit error code (<c>PT_ERROR</c>).
    /// </summary>
    ErrorCode = 0x000A,

    /// <summary>
    /// A Boolean value (<c>PT_BOOLEAN</c>).
    /// </summary>
    Boolean = 0x000B,

    /// <summary>
    /// An embedded object reference (<c>PT_OBJECT</c>); the payload is a storage, not an inline value.
    /// </summary>
    Object = 0x000D,

    /// <summary>
    /// A signed 64-bit integer (<c>PT_LONGLONG</c> / <c>PT_I8</c>).
    /// </summary>
    Int64 = 0x0014,

    /// <summary>
    /// A code-page (ANSI) string (<c>PT_STRING8</c>).
    /// </summary>
    String8 = 0x001E,

    /// <summary>
    /// A UTF-16LE string (<c>PT_UNICODE</c>).
    /// </summary>
    Unicode = 0x001F,

    /// <summary>
    /// A FILETIME time stamp (<c>PT_SYSTIME</c>).
    /// </summary>
    SystemTime = 0x0040,

    /// <summary>
    /// A 16-byte GUID (<c>PT_CLSID</c>).
    /// </summary>
    Guid = 0x0048,

    /// <summary>
    /// An opaque byte sequence (<c>PT_BINARY</c>).
    /// </summary>
    Binary = 0x0102,
}
