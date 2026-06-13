// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

/// <summary>
/// Provides sign, magnitude, and related convenience members for the runtime-tagged <see cref="Money" /> type, keeping
/// those derived helpers off the core value type while preserving familiar property-style access.
/// </summary>
/// <remarks>
/// <para>
/// Each member is a thin projection over <see cref="Money.Amount" /> and the public arithmetic surface, carrying no
/// state of its own. Splitting them into this extension surface keeps <see cref="Money" /> focused on construction,
/// arithmetic, equality, and formatting.
/// </para>
/// <para>
/// When compiled with a tool-chain that supports C# 14 extension members the helpers are exposed as extension
/// properties (for example <c>money.IsZero</c>); otherwise they compile as classic extension methods (for example
/// <c>money.IsZero()</c>).
/// </para>
/// </remarks>
public static partial class MoneyExtensions
{
}
