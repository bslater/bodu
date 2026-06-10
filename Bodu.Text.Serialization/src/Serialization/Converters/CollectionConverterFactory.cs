// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Produces a <see cref="CollectionConverter{TCollection, TElement}" /> for single-dimensional arrays, for
/// <see cref="System.Collections.Generic.List{T}" /> and the interfaces it satisfies, and for concrete collection types
/// that implement <see cref="System.Collections.Generic.ICollection{T}" /> with a public parameterless constructor.
/// </summary>
internal sealed class CollectionConverterFactory
    : FormatConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return TryGetInfo(typeToConvert, out _, out _);
    }

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        _ = TryGetInfo(typeToConvert, out Type? elementType, out CollectionStrategy strategy);
        FormatConverter elementConverter = options.GetConverter(elementType!);
        Type converterType = typeof(CollectionConverter<,>).MakeGenericType(typeToConvert, elementType!);
        return (FormatConverter)Activator.CreateInstance(converterType, elementConverter, strategy) !;
    }

    /// <summary>
    /// Determines whether a type is a supported collection and, if so, its element type and materialization strategy.
    /// </summary>
    /// <param name="type">The candidate collection type.</param>
    /// <param name="elementType">When this method returns <see langword="true" />, the element type.</param>
    /// <param name="strategy">When this method returns <see langword="true" />, the materialization strategy.</param>
    /// <returns>
    /// <see langword="true" /> when the type is a supported collection; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetInfo(Type type, out Type? elementType, out CollectionStrategy strategy)
    {
        elementType = null;
        strategy = CollectionStrategy.ListAssignable;

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                return false;

            elementType = type.GetElementType();
            strategy = CollectionStrategy.Array;
            return true;
        }

        Type? enumerable = FindEnumerableInterface(type);
        if (enumerable is null)
            return false;

        elementType = enumerable.GetGenericArguments()[0];

        Type listType = typeof(List<>).MakeGenericType(elementType);
        if (type.IsInterface && type.IsAssignableFrom(listType))
        {
            strategy = CollectionStrategy.ListAssignable;
            return true;
        }

        Type collectionType = typeof(ICollection<>).MakeGenericType(elementType);
        if (!type.IsAbstract && !type.IsInterface && collectionType.IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            strategy = CollectionStrategy.ConcreteCollection;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the single <see cref="IEnumerable{T}" /> interface implemented by a type, if exactly one exists.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>
    /// The closed <see cref="IEnumerable{T}" /> interface, or <see langword="null" /> when none or several exist.
    /// </returns>
    private static Type? FindEnumerableInterface(Type type)
    {
        Type? found = null;
        foreach (Type candidate in EnumerateSelfAndInterfaces(type))
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                if (found is not null)
                    return null;

                found = candidate;
            }
        }

        return found;
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
