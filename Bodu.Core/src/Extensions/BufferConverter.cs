// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides methods for copying and converting between raw byte arrays and arrays of unmanaged types.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BufferConverter"/> class enables efficient reinterpretation of binary data as strongly-typed arrays without
/// allocating additional memory or performing per-element conversions. Operations assume platform-native endianness.
/// </para>
/// <para>Only types that are <see langword="unmanaged"/> (e.g., primitives, structs without references) are supported.</para>
/// </remarks>
public static partial class BufferConverter
{
}