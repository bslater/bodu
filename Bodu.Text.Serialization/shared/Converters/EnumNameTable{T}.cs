// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNameTable{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using Bodu.Text.Serialization;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Maps the members of an enumeration to and from the names a by-name enumeration converter reads and writes, as
/// System.Text.Json's <c>JsonStringEnumConverter</c> maps them.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// Each member has a wire name: the name its <see cref="StringEnumMemberNameAttribute" /> configures, or else the name
/// the naming policy makes of its CLR name, or else its CLR name. A wire name binds only as written, in its own case. A
/// member without the attribute also binds by its CLR name, in any case, while a member the attribute renames binds by
/// its configured name alone.
/// </para>
/// <para>
/// Text that is not a whole wire name is read as a comma-separated list of names, each trimmed of white space, whose
/// members' values are combined, so a combination of flags binds with its names in any order. A trailing comma is
/// tolerated, but an empty name anywhere else is not, and the list holds names only: an integer binds, where the
/// converter allows integer values, only as the whole text.
/// </para>
/// <para>
/// On write a member is written by its wire name. Any other value is formatted as <see cref="Enum.ToString()" />
/// formats it, and when that is the comma-separated list of member names of a combination of defined flags, each CLR
/// name is replaced by its member's wire name, so the flags are written in the order the runtime lists them.
/// </para>
/// </remarks>
internal sealed class EnumNameTable<T>
    where T : struct, Enum
{
    /// <summary>The separator between the names of a combination of flags, as the runtime writes it.</summary>
    private const string FlagSeparator = ", ";

    /// <summary>Maps each wire name to its member's value, matched in its own case.</summary>
    private readonly Dictionary<string, T> _wireNameToValue = new(StringComparer.Ordinal);

    /// <summary>Maps the CLR name of each member without a configured name to its value, matched in any case.</summary>
    private readonly Dictionary<string, T> _clrNameToValue = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps each value to the wire name it is written as; the first member wins when several share a value.</summary>
    private readonly Dictionary<T, string> _valueToWireName = new();

    /// <summary>Maps each member's CLR name to its wire name, to rename the flags the runtime lists by CLR name.</summary>
    private readonly Dictionary<string, string> _clrNameToWireName = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumNameTable{T}" /> class.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <remarks>
    /// The maps are built once, by reflecting over the public, static fields of <typeparamref name="T" />, resolving
    /// each member's wire name from its <see cref="StringEnumMemberNameAttribute" />, then the naming policy, and
    /// finally the CLR field name.
    /// </remarks>
    public EnumNameTable(NamingPolicy? namingPolicy)
    {
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string? configuredName = field.GetCustomAttribute<StringEnumMemberNameAttribute>()?.Name;
            string wireName = configuredName ?? namingPolicy?.ConvertName(field.Name) ?? field.Name;
            var value = (T)field.GetValue(null)!;

            _wireNameToValue[wireName] = value;
            _valueToWireName.TryAdd(value, wireName);
            _clrNameToWireName[field.Name] = wireName;
            if (configuredName is null)
                _clrNameToValue.TryAdd(field.Name, value);
        }
    }

    /// <summary>
    /// Attempts to read text as a wire name, or as a comma-separated list of names whose members' values combine.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the member the text names or the combination of the members
    /// its list names; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is a wire name, or a list each of whose names is a wire
    /// name or the CLR name of a member without a configured name; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryParse(string text, out T value)
    {
        if (_wireNameToValue.TryGetValue(text, out value))
            return true;

        ulong bits = 0;
        var named = false;
        ReadOnlySpan<char> rest = text;
        while (true)
        {
            int separator = rest.IndexOf(',');
            ReadOnlySpan<char> name = (separator < 0 ? rest : rest[..separator]).Trim();
            if (name.IsEmpty)
            {
                // Only the name after a trailing comma may be empty, as JsonStringEnumConverter allows.
                if (separator >= 0 || !named)
                {
                    value = default;
                    return false;
                }

                break;
            }

            string key = name.Length == text.Length ? text : name.ToString();
            if (!_wireNameToValue.TryGetValue(key, out T member) && !_clrNameToValue.TryGetValue(key, out member))
            {
                value = default;
                return false;
            }

            bits |= ToBits(member);
            named = true;
            if (separator < 0)
                break;

            rest = rest[(separator + 1)..];
        }

        value = FromBits(bits);
        return true;
    }

    /// <summary>
    /// Attempts to format a value by the wire names of the member it is, or of the flags it combines.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="text">
    /// When this method returns <see langword="true" />, the value's wire name or the comma-separated wire names of its
    /// flags; otherwise, the value's decimal text, as the runtime formats it.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is a member or a combination of defined flags; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool TryFormat(T value, out string text)
    {
        if (_valueToWireName.TryGetValue(value, out string? name))
        {
            text = name;
            return true;
        }

        // The runtime formats a value that is neither a member nor a combination of defined flags as its decimal text,
        // and a combination of flags as its members' CLR names.
        text = value.ToString();
        if (EnumIntegerConversion.ContainsIntegerText(text))
            return false;

        string[] names = text.Split(FlagSeparator);
        for (var i = 0; i < names.Length; i++)
            names[i] = _clrNameToWireName[names[i]];

        text = string.Join(FlagSeparator, names);
        return true;
    }

    /// <summary>
    /// Returns the bits of an enumeration value, zero-extended to 64 bits, so that the values of several members
    /// combine.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value's underlying bits.</returns>
    private static ulong ToBits(T value) => Unsafe.SizeOf<T>() switch
    {
        1 => Unsafe.As<T, byte>(ref value),
        2 => Unsafe.As<T, ushort>(ref value),
        4 => Unsafe.As<T, uint>(ref value),
        _ => Unsafe.As<T, ulong>(ref value),
    };

    /// <summary>
    /// Returns the enumeration value whose underlying bits are the low bits of a combination built by
    /// <see cref="ToBits(T)" />.
    /// </summary>
    /// <param name="bits">The combined bits.</param>
    /// <returns>The value with those bits.</returns>
    private static T FromBits(ulong bits)
    {
        switch (Unsafe.SizeOf<T>())
        {
            case 1:
                var bits8 = (byte)bits;
                return Unsafe.As<byte, T>(ref bits8);

            case 2:
                var bits16 = (ushort)bits;
                return Unsafe.As<ushort, T>(ref bits16);

            case 4:
                var bits32 = (uint)bits;
                return Unsafe.As<uint, T>(ref bits32);

            default:
                return Unsafe.As<ulong, T>(ref bits);
        }
    }
}
