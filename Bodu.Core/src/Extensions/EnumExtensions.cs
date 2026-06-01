// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides non-boxing bitwise helpers for flag-style enumerations — membership tests and add, remove, and toggle
/// operations that work uniformly across every enum underlying type.
/// </summary>
/// <remarks>
/// <para>
/// The built-in <see cref="System.Enum.HasFlag(System.Enum)" /> boxes both operands on every call, and the bitwise
/// operators (<c>|</c>, <c>&amp;</c>, <c>^</c>, <c>~</c>) are unavailable on an open generic enum type. This class
/// supplies generic, allocation-free equivalents constrained to <c>struct, System.Enum</c>.
/// </para>
/// <para>
/// Each operation reinterprets the enum value as its underlying integer, applies the bitwise operation, and — for the
/// mutating helpers — reinterprets the result back to the enum type. No boxing, reflection, or
/// <see cref="System.Convert" /> call is involved, so the helpers are safe to use on hot paths.
/// </para>
/// <para>
/// These helpers are intended for enumerations annotated with <see cref="System.FlagsAttribute" />; they operate purely
/// on the bit pattern and do not validate that the supplied values correspond to declared members.
/// </para>
/// </remarks>
public static partial class EnumExtensions
{
}
