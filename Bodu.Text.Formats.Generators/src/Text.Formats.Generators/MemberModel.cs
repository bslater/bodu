// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Describes one mapped property of an annotated type: its CLR name, resolved wire name, and scalar conversion
/// strategy.
/// </summary>
/// <param name="PropertyName">The CLR property name.</param>
/// <param name="WireName">The resolved column or key name, honouring <c>[PropertyName]</c>.</param>
/// <param name="Scalar">The scalar conversion strategy for the property type.</param>
/// <param name="IsNullable">Whether the property type is a <see cref="Nullable{T}" /> value type.</param>
/// <param name="TypeDisplay">
/// The fully qualified display of the property's effective (nullable-unwrapped) type, used in emitted casts and parse
/// calls.
/// </param>
/// <param name="IgnoreCondition">
/// The numeric value of the write-time condition the property's own <c>[Ignore]</c> sets, or <see langword="null" />
/// when the property has none.
/// </param>
internal sealed record MemberModel(
    string PropertyName,
    string WireName,
    ScalarKind Scalar,
    bool IsNullable,
    string TypeDisplay,
    int? IgnoreCondition);
