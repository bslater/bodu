// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateImportUse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Describes a single concept cherry-picked from an imported resource, with optional rename and per-import overrides.
/// </summary>
/// <param name="NotableDateRef">The identifier of the concept to import from the source resource.</param>
/// <param name="As">An identifier to rename the imported concept to, or <see langword="null" /> to keep its identifier.</param>
/// <param name="Territory">A territory to assign to every rule of the imported concept, or <see langword="null" /> to keep theirs.</param>
/// <param name="Category">A category to override the imported concept with, or <see langword="null" /> to keep its category.</param>
/// <param name="NonWorking">
/// An override for the imported concept's default non-working flag, or <see langword="null" /> to keep it.
/// </param>
/// <param name="AdjustmentPolicyRefs">
/// The adjustment policy ids that replace those on every rule of the imported concept, or <see langword="null" /> to
/// keep the source rules' adjustments. An empty list clears the source adjustments.
/// </param>
internal sealed record NotableDateImportUse(
    string NotableDateRef,
    string? As,
    string? Territory,
    NotableDateCategory? Category,
    bool? NonWorking,
    IReadOnlyList<string>? AdjustmentPolicyRefs);
