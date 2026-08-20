// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstWireType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Classifies the LTP's 16-bit property wire type codes (the MS-OXCDATA <c>Ptyp*</c> values) by how a property
/// context or table context stores their payloads: inline in the value dword, in a fixed-size heap item, or behind an
/// <c>HNID</c> as variable-size data.
/// </summary>
/// <remarks>
/// This layer deliberately carries no MAPI semantics — the codes stay raw <see langword="ushort" /> values; the
/// classification only records each type's storage width so values can be resolved without misreading the file.
/// </remarks>
internal static class PstWireType
{
    /// <summary>The flag bit that marks a multi-valued form of a base wire type.</summary>
    private const ushort MultiValuedFlag = 0x1000;

    /// <summary>
    /// Attempts to classify a wire type as one stored inline in the value dword.
    /// </summary>
    /// <param name="wireType">The wire type code.</param>
    /// <param name="size">When this method returns <see langword="true" />, the value's width in bytes (0 to 4).</param>
    /// <returns><see langword="true" /> when the type is stored inline.</returns>
    internal static bool TryGetInlineSize(ushort wireType, out int size)
    {
        switch (wireType)
        {
            case 0x0000: // PtypUnspecified
            case 0x0001: // PtypNull
                size = 0;
                return true;

            case 0x0002: // PtypInteger16
                size = 2;
                return true;

            case 0x0003: // PtypInteger32
            case 0x0004: // PtypFloating32
            case 0x000A: // PtypErrorCode
                size = 4;
                return true;

            case 0x000B: // PtypBoolean
                size = 1;
                return true;

            default:
                size = 0;
                return false;
        }
    }

    /// <summary>
    /// Attempts to classify a wire type as one stored in a fixed-size heap item addressed by the value dword.
    /// </summary>
    /// <param name="wireType">The wire type code.</param>
    /// <param name="size">When this method returns <see langword="true" />, the value's width in bytes (8 or 16).</param>
    /// <returns><see langword="true" /> when the type is stored as a fixed-size heap item.</returns>
    internal static bool TryGetFixedHeapSize(ushort wireType, out int size)
    {
        switch (wireType)
        {
            case 0x0005: // PtypFloating64
            case 0x0006: // PtypCurrency
            case 0x0007: // PtypFloatingTime
            case 0x0014: // PtypInteger64
            case 0x0040: // PtypTime
                size = 8;
                return true;

            case 0x0048: // PtypGuid
                size = 16;
                return true;

            default:
                size = 0;
                return false;
        }
    }

    /// <summary>
    /// Determines whether a wire type is one this reader recognizes: the inline and fixed-size sets, the
    /// variable-size scalar types, and every multi-valued form of a recognized base type.
    /// </summary>
    /// <param name="wireType">The wire type code.</param>
    /// <returns><see langword="true" /> when the type's storage classification is known.</returns>
    internal static bool IsKnown(ushort wireType)
    {
        if ((wireType & MultiValuedFlag) != 0)
            return IsKnownBase((ushort)(wireType & ~MultiValuedFlag));

        return TryGetInlineSize(wireType, out _) || TryGetFixedHeapSize(wireType, out _) || IsVariable(wireType);
    }

    /// <summary>
    /// Determines whether a wire type is stored as variable-size data behind an <c>HNID</c>.
    /// </summary>
    /// <param name="wireType">The wire type code.</param>
    /// <returns><see langword="true" /> when the type is a recognized variable-size scalar.</returns>
    internal static bool IsVariable(ushort wireType) => wireType switch
    {
        0x000D => true, // PtypObject
        0x001E => true, // PtypString8
        0x001F => true, // PtypString (UTF-16LE)
        0x0102 => true, // PtypBinary
        _ => false,
    };

    /// <summary>
    /// Determines whether a multi-valued form's base type is recognized.
    /// </summary>
    /// <param name="baseType">The base wire type with the multi-valued flag cleared.</param>
    /// <returns><see langword="true" /> when the base type is recognized.</returns>
    private static bool IsKnownBase(ushort baseType) =>
        TryGetInlineSize(baseType, out _) || TryGetFixedHeapSize(baseType, out _) || IsVariable(baseType);
}
