// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides non-boxing bitwise helpers for flag-style enumerations - membership tests and add, remove, and toggle
/// operations that work uniformly across every enum underlying type - and description and display-name lookups for
/// enumeration values.
/// </summary>
/// <remarks>
/// <para>
/// The built-in <see cref="System.Enum.HasFlag(System.Enum)" /> boxes both operands on every call, and the bitwise
/// operators (<c>|</c>, <c>&amp;</c>, <c>^</c>, <c>~</c>) are unavailable on an open generic enum type. This class
/// supplies generic, allocation-free equivalents constrained to <c>struct, System.Enum</c>.
/// </para>
/// <para>
/// Each flag operation (<c>HasAllFlags</c>, <c>HasAnyFlag</c>, <c>SetFlag</c>, <c>ClearFlag</c>, <c>ToggleFlag</c>)
/// reinterprets the enum value as its underlying integer, applies the bitwise operation, and - for the mutating
/// helpers - reinterprets the result back to the enum type. No boxing, reflection, or <see cref="System.Convert" />
/// call is involved, so the flag helpers are safe to use on hot paths.
/// </para>
/// <para>
/// The flag helpers are intended for enumerations annotated with <see cref="System.FlagsAttribute" />; they operate
/// purely on the bit pattern and do not validate that the supplied values correspond to declared members.
/// </para>
/// <para>
/// <c>GetDescription</c>, <c>TryGetDescription</c>, and <c>GetDisplayName</c> are different: they read the
/// <see cref="System.ComponentModel.DescriptionAttribute" /> and
/// <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" /> of the enumeration's members through
/// reflection, cached once per enumeration type by <see cref="Enums" />, and fall back to the value's
/// <see cref="object.ToString" /> text.
/// </para>
/// </remarks>
public static partial class EnumExtensions
{
}
