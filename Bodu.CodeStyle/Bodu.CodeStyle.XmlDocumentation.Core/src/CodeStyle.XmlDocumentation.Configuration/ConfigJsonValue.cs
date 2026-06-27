// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigJsonValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Represents a single value parsed from a configuration JSON document by <see cref="ConfigJsonParser" />. This is a
/// deliberately small, dependency-free model used in place of <c>System.Text.Json</c> so the analyzer package carries
/// no external runtime dependency.
/// </summary>
internal sealed class ConfigJsonValue
{
    /// <summary>The numeric content; meaningful only when <see cref="Kind" /> is <see cref="ConfigJsonValueKind.Number" />.</summary>
    private readonly double _number;

    /// <summary>The boolean content; meaningful only when <see cref="Kind" /> is <see cref="ConfigJsonValueKind.Boolean" />.</summary>
    private readonly bool _boolean;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigJsonValue" /> class.
    /// </summary>
    /// <param name="kind">The kind of value being created.</param>
    /// <param name="stringValue">The string content, or <see langword="null" /> for non-string values.</param>
    /// <param name="number">The numeric content, or zero for non-number values.</param>
    /// <param name="boolean">The boolean content, or <see langword="false" /> for non-boolean values.</param>
    /// <param name="members">The object member map, or <see langword="null" /> for non-object values.</param>
    /// <param name="items">The array item list, or <see langword="null" /> for non-array values.</param>
    private ConfigJsonValue(
        ConfigJsonValueKind kind,
        string? stringValue,
        double number,
        bool boolean,
        IReadOnlyDictionary<string, ConfigJsonValue>? members,
        IReadOnlyList<ConfigJsonValue>? items)
    {
        this.Kind = kind;
        this.StringValue = stringValue;
        this._number = number;
        this._boolean = boolean;
        this.Members = members;
        this.Items = items;
    }

    /// <summary>
    /// Gets the kind of this value.
    /// </summary>
    /// <value>The value kind.</value>
    public ConfigJsonValueKind Kind { get; }

    /// <summary>
    /// Gets the string content when <see cref="Kind" /> is <see cref="ConfigJsonValueKind.String" />.
    /// </summary>
    /// <value>The string content, or <see langword="null" /> for non-string values.</value>
    public string? StringValue { get; }

    /// <summary>
    /// Gets the object members when <see cref="Kind" /> is <see cref="ConfigJsonValueKind.Object" />.
    /// </summary>
    /// <value>The member map, or <see langword="null" /> for non-object values.</value>
    public IReadOnlyDictionary<string, ConfigJsonValue>? Members { get; }

    /// <summary>
    /// Gets the array items when <see cref="Kind" /> is <see cref="ConfigJsonValueKind.Array" />.
    /// </summary>
    /// <value>The item list, or <see langword="null" /> for non-array values.</value>
    public IReadOnlyList<ConfigJsonValue>? Items { get; }

    /// <summary>
    /// Gets a value indicating whether gets the boolean content when <see cref="Kind" /> is
    /// <see cref="ConfigJsonValueKind.Boolean" />.
    /// </summary>
    /// <value>The boolean content; <see langword="false" /> for non-boolean values.</value>
    public bool BooleanValue => this._boolean;

    /// <summary>
    /// Creates an object value.
    /// </summary>
    /// <param name="members">The member map.</param>
    /// <returns>A new object value.</returns>
    public static ConfigJsonValue ForObject(IReadOnlyDictionary<string, ConfigJsonValue> members) =>
        new ConfigJsonValue(ConfigJsonValueKind.Object, null, 0, false, members, null);

    /// <summary>
    /// Creates an array value.
    /// </summary>
    /// <param name="items">The item list.</param>
    /// <returns>A new array value.</returns>
    public static ConfigJsonValue ForArray(IReadOnlyList<ConfigJsonValue> items) =>
        new ConfigJsonValue(ConfigJsonValueKind.Array, null, 0, false, null, items);

    /// <summary>
    /// Creates a string value.
    /// </summary>
    /// <param name="value">The string content.</param>
    /// <returns>A new string value.</returns>
    public static ConfigJsonValue ForString(string value) =>
        new ConfigJsonValue(ConfigJsonValueKind.String, value, 0, false, null, null);

    /// <summary>
    /// Creates a number value.
    /// </summary>
    /// <param name="value">The numeric content.</param>
    /// <returns>A new number value.</returns>
    public static ConfigJsonValue ForNumber(double value) =>
        new ConfigJsonValue(ConfigJsonValueKind.Number, null, value, false, null, null);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    /// <param name="value">The boolean content.</param>
    /// <returns>A new boolean value.</returns>
    public static ConfigJsonValue ForBoolean(bool value) =>
        new ConfigJsonValue(ConfigJsonValueKind.Boolean, null, 0, value, null, null);

    /// <summary>
    /// Creates a null value.
    /// </summary>
    /// <returns>A new null value.</returns>
    public static ConfigJsonValue ForNull() =>
        new ConfigJsonValue(ConfigJsonValueKind.Null, null, 0, false, null, null);

    /// <summary>
    /// Attempts to read this value as a 32-bit integer.
    /// </summary>
    /// <param name="value">When this method returns, the integer value when successful; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when this is a number with an exact 32-bit integer value; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryGetInt32(out int value)
    {
        if (this.Kind == ConfigJsonValueKind.Number &&
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
