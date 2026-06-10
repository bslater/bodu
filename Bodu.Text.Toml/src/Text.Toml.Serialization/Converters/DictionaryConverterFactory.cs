// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Produces a <see cref="DictionaryConverter{TDictionary, TValue}" /> for string-keyed dictionaries — concrete
/// dictionaries with a public parameterless constructor and the dictionary interfaces that
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> satisfies.
/// </summary>
internal sealed class DictionaryConverterFactory
    : TomlConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return TryGetInfo(typeToConvert, out _, out _);
    }

    /// <inheritdoc />
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        _ = TryGetInfo(typeToConvert, out Type? valueType, out var concrete);
        TomlConverter valueConverter = options.GetConverter(valueType!);
        Type converterType = typeof(DictionaryConverter<,>).MakeGenericType(typeToConvert, valueType!);
        return (TomlConverter)Activator.CreateInstance(converterType, valueConverter, concrete) !;
    }

    /// <summary>
    /// Determines whether a type is a supported string-keyed dictionary and, if so, its value type and whether it is
    /// concrete.
    /// </summary>
    /// <param name="type">The candidate dictionary type.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the value type.</param>
    /// <param name="concrete">
    /// When this method returns <see langword="true" />, whether the type is a concrete dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the type is a supported dictionary; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetInfo(Type type, out Type? valueType, out bool concrete)
    {
        valueType = null;
        concrete = false;

        Type? dictionaryInterface = FindStringKeyedDictionary(type);
        if (dictionaryInterface is null)
            return false;

        valueType = dictionaryInterface.GetGenericArguments()[1];

        Type closedDictionary = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
        if (type.IsInterface && type.IsAssignableFrom(closedDictionary))
        {
            concrete = false;
            return true;
        }

        Type mutableInterface = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
        if (!type.IsAbstract && !type.IsInterface && mutableInterface.IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            concrete = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds a string-keyed dictionary interface implemented by the type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// The closed dictionary interface with a string key, or <see langword="null" /> when none exists.
    /// </returns>
    private static Type? FindStringKeyedDictionary(Type type)
    {
        foreach (Type candidate in EnumerateSelfAndInterfaces(type))
        {
            if (!candidate.IsGenericType)
                continue;

            Type definition = candidate.GetGenericTypeDefinition();
            if ((definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                && candidate.GetGenericArguments()[0] == typeof(string))
            {
                return candidate;
            }
        }

        return null;
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
