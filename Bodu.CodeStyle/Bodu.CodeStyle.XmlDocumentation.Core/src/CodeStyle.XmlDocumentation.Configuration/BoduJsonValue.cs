// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduJsonValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Represents a single value parsed from a configuration JSON document by <see cref="BoduJsonParser" />. This is
/// a deliberately small, dependency-free model used in place of <c>System.Text.Json</c> so the analyzer package
/// carries no external runtime dependency.
/// </summary>
internal sealed class BoduJsonValue
{
    private readonly double _number;
    private readonly bool _boolean;

    private BoduJsonValue(
        BoduJsonValueKind kind,
        string? stringValue,
        double number,
        bool boolean,
        IReadOnlyDictionary<string, BoduJsonValue>? members,
        IReadOnlyList<BoduJsonValue>? items)
    {
        this.Kind = kind;
        this.StringValue = stringValue;
        this._number = number;
        this._boolean = boolean;
        this.Members = members;
        this.Items = items;
    }

    /// <summary>Gets the kind of this value.</summary>
    /// <returns>The value kind.</returns>
    public BoduJsonValueKind Kind { get; }

    /// <summary>Gets the string content when <see cref="Kind" /> is <see cref="BoduJsonValueKind.String" />.</summary>
    /// <returns>The string content, or <see langword="null" /> for non-string values.</returns>
    public string? StringValue { get; }

    /// <summary>Gets the object members when <see cref="Kind" /> is <see cref="BoduJsonValueKind.Object" />.</summary>
    /// <returns>The member map, or <see langword="null" /> for non-object values.</returns>
    public IReadOnlyDictionary<string, BoduJsonValue>? Members { get; }

    /// <summary>Gets the array items when <see cref="Kind" /> is <see cref="BoduJsonValueKind.Array" />.</summary>
    /// <returns>The item list, or <see langword="null" /> for non-array values.</returns>
    public IReadOnlyList<BoduJsonValue>? Items { get; }

    /// <summary>Gets the boolean content when <see cref="Kind" /> is <see cref="BoduJsonValueKind.Boolean" />.</summary>
    /// <returns>The boolean content; <see langword="false" /> for non-boolean values.</returns>
    public bool BooleanValue => this._boolean;

    /// <summary>Creates an object value.</summary>
    /// <param name="members">The member map.</param>
    /// <returns>A new object value.</returns>
    public static BoduJsonValue ForObject(IReadOnlyDictionary<string, BoduJsonValue> members) =>
        new BoduJsonValue(BoduJsonValueKind.Object, null, 0, false, members, null);

    /// <summary>Creates an array value.</summary>
    /// <param name="items">The item list.</param>
    /// <returns>A new array value.</returns>
    public static BoduJsonValue ForArray(IReadOnlyList<BoduJsonValue> items) =>
        new BoduJsonValue(BoduJsonValueKind.Array, null, 0, false, null, items);

    /// <summary>Creates a string value.</summary>
    /// <param name="value">The string content.</param>
    /// <returns>A new string value.</returns>
    public static BoduJsonValue ForString(string value) =>
        new BoduJsonValue(BoduJsonValueKind.String, value, 0, false, null, null);

    /// <summary>Creates a number value.</summary>
    /// <param name="value">The numeric content.</param>
    /// <returns>A new number value.</returns>
    public static BoduJsonValue ForNumber(double value) =>
        new BoduJsonValue(BoduJsonValueKind.Number, null, value, false, null, null);

    /// <summary>Creates a boolean value.</summary>
    /// <param name="value">The boolean content.</param>
    /// <returns>A new boolean value.</returns>
    public static BoduJsonValue ForBoolean(bool value) =>
        new BoduJsonValue(BoduJsonValueKind.Boolean, null, 0, value, null, null);

    /// <summary>Creates a null value.</summary>
    /// <returns>A new null value.</returns>
    public static BoduJsonValue ForNull() =>
        new BoduJsonValue(BoduJsonValueKind.Null, null, 0, false, null, null);

    /// <summary>
    /// Attempts to read this value as a 32-bit integer.
    /// </summary>
    /// <param name="value">When this method returns, the integer value when successful; otherwise zero.</param>
    /// <returns><see langword="true" /> when this is a number with an exact 32-bit integer value; otherwise <see langword="false" />.</returns>
    public bool TryGetInt32(out int value)
    {
        if (this.Kind == BoduJsonValueKind.Number &&
            this._number >= int.MinValue &&
            this._number <= int.MaxValue &&
            this._number == System.Math.Floor(this._number))
        {
            value = (int)this._number;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Returns the invariant string form of a number value, used only for diagnostics.
    /// </summary>
    /// <returns>The number formatted with the invariant culture.</returns>
    public string NumberToString() =>
        this._number.ToString(CultureInfo.InvariantCulture);
}
