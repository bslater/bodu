// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNullHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Specifies how <see cref="BencodeSerializer" /> treats a property whose value is <see langword="null" /> when
/// writing.
/// </summary>
/// <remarks>
/// Bencode has no representation for a null value, so a member whose value is <see langword="null" /> is always omitted
/// from the output regardless of this setting; the enumeration is retained to mirror
/// <see cref="System.Text.Json.JsonSerializerOptions.DefaultIgnoreCondition" /> and to keep the options surface aligned
/// with the other Bodu format serializers.
/// </remarks>
public enum BencodeNullHandling
{
    /// <summary>
    /// Omit a property whose value is <see langword="null" /> from the output. This is the default.
    /// </summary>
    IgnoreOnWrite,

    /// <summary>
    /// Request that a null value be written for the property. Bencode cannot represent null, so the property is omitted
    /// in practice.
    /// </summary>
    Include,
}
