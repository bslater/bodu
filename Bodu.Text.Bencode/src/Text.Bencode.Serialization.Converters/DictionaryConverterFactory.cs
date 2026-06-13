// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces a <see cref="DictionaryConverter{TDictionary, TKey, TValue}" /> for dictionaries with a supported key type
/// — concrete dictionaries with a public parameterless constructor and the dictionary interfaces that
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> satisfies.
/// </summary>
/// <remarks>
/// Supported key types are <see cref="string" />, the fixed-width integer family (<see cref="sbyte" />,
/// <see cref="byte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />,
/// <see cref="long" />, <see cref="ulong" />), enumerations, <see cref="Guid" />, <see cref="bool" />, and
/// <see cref="char" /> — the key types whose text form round-trips cleanly through a Bencode byte-string key. A
/// dictionary with any other key type is not claimed by this factory and falls through to the later converters.
/// </remarks>
internal sealed class DictionaryConverterFactory
    : BencodeConverterFactory
{
    /// <summary>
    /// The non-enum key types this factory supports, mapped to their key-conversion kind.
    /// </summary>
    private static readonly Dictionary<Type, DictionaryKeyKind> s_keyKinds = new()
    {
        [typeof(string)] = DictionaryKeyKind.String,
        [typeof(sbyte)] = DictionaryKeyKind.Integer,
        [typeof(byte)] = DictionaryKeyKind.Integer,
        [typeof(short)] = DictionaryKeyKind.Integer,
        [typeof(ushort)] = DictionaryKeyKind.Integer,
        [typeof(int)] = DictionaryKeyKind.Integer,
        [typeof(uint)] = DictionaryKeyKind.Integer,
        [typeof(long)] = DictionaryKeyKind.Integer,
        [typeof(ulong)] = DictionaryKeyKind.Integer,
        [typeof(Guid)] = DictionaryKeyKind.Guid,
        [typeof(bool)] = DictionaryKeyKind.Boolean,
        [typeof(char)] = DictionaryKeyKind.Char,
    };

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return TryGetInfo(typeToConvert, out _, out _, out _, out _);
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        _ = TryGetInfo(typeToConvert, out Type? keyType, out Type? valueType, out DictionaryKeyKind keyKind, out var concrete);
        BencodeConverter valueConverter = options.GetConverter(valueType!);
        Type converterType = typeof(DictionaryConverter<,,>).MakeGenericType(typeToConvert, keyType!, valueType!);
        return (BencodeConverter)Activator.CreateInstance(converterType, valueConverter, keyKind, concrete) !;
    }

    /// <summary>
    /// Determines whether a type is a supported dictionary and, if so, its key and value types, its key-conversion
    /// kind, and whether it is concrete.
    /// </summary>
    /// <param name="type">The candidate dictionary type.</param>
    /// <param name="keyType">When this method returns <see langword="true" />, the key type.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the value type.</param>
    /// <param name="keyKind">When this method returns <see langword="true" />, the key-conversion kind.</param>
    /// <param name="concrete">
    /// When this method returns <see langword="true" />, whether the type is a concrete dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the type is a supported dictionary; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetInfo(Type type, out Type? keyType, out Type? valueType, out DictionaryKeyKind keyKind, out bool concrete)
    {
        keyType = null;
        valueType = null;
        keyKind = DictionaryKeyKind.String;
        concrete = false;

        Type? dictionaryInterface = FindSupportedDictionaryInterface(type, out keyKind);
        if (dictionaryInterface is null)
            return false;

        keyType = dictionaryInterface.GetGenericArguments()[0];
        valueType = dictionaryInterface.GetGenericArguments()[1];

        Type closedDictionary = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        if (type.IsInterface && type.IsAssignableFrom(closedDictionary))
        {
            concrete = false;
            return true;
        }

        Type mutableInterface = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
        if (!type.IsAbstract && !type.IsInterface && mutableInterface.IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            concrete = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds a dictionary interface with a supported key type implemented by the type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="keyKind">
    /// When this method returns non-<see langword="null" />, the key-conversion kind for the interface's key type.
    /// </param>
    /// <returns>
    /// The closed dictionary interface whose key type is supported, or <see langword="null" /> when none exists.
    /// </returns>
    private static Type? FindSupportedDictionaryInterface(Type type, out DictionaryKeyKind keyKind)
    {
        keyKind = DictionaryKeyKind.String;

        foreach (Type candidate in EnumerateSelfAndInterfaces(type))
        {
            if (!candidate.IsGenericType)
                continue;

            Type definition = candidate.GetGenericTypeDefinition();
            if (definition != typeof(IDictionary<,>) && definition != typeof(IReadOnlyDictionary<,>))
                continue;

            if (TryGetKeyKind(candidate.GetGenericArguments()[0], out keyKind))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Determines whether a key type is supported and, if so, its key-conversion kind.
    /// </summary>
    /// <param name="keyType">The candidate key type.</param>
    /// <param name="keyKind">When this method returns <see langword="true" />, the key-conversion kind.</param>
    /// <returns><see langword="true" /> when the key type is supported; otherwise <see langword="false" />.</returns>
    private static bool TryGetKeyKind(Type keyType, out DictionaryKeyKind keyKind)
    {
        if (keyType.IsEnum)
        {
            keyKind = DictionaryKeyKind.Enum;
            return true;
        }

        return s_keyKinds.TryGetValue(keyType, out keyKind);
    }

    /// <summary>
    /// Enumerates a type together with the interfaces it implements.
    /// </summary>
    /// <param name="type">The type to enumerate.</param>
    /// <returns>The type and its interfaces.</returns>
    private static IEnumerable<Type> EnumerateSelfAndInterfaces(Type type)
    {
        if (type.IsInterface)
            yield return type;

        foreach (Type @interface in type.GetInterfaces())
            yield return @interface;
    }
}
