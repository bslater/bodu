// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces a <see cref="CollectionConverter{TCollection, TElement}" /> for single-dimensional arrays, for
/// <see cref="System.Collections.Generic.List{T}" /> and the interfaces it satisfies, for concrete collection types
/// that implement <see cref="System.Collections.Generic.ICollection{T}" /> with a public parameterless constructor, and
/// for the queue-, stack-, and bag-shaped collections <see cref="Queue{T}" />, <see cref="Stack{T}" />,
/// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" />,
/// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" />, and
/// <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> (including subclasses with a public parameterless
/// constructor), which do not implement <see cref="System.Collections.Generic.ICollection{T}" /> and are materialized
/// through <c>Enqueue</c>, <c>Push</c>, or <c>Add</c> instead.
/// </summary>
internal sealed class CollectionConverterFactory
    : BencodeConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return TryGetInfo(typeToConvert, out _, out _);
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        _ = TryGetInfo(typeToConvert, out Type? elementType, out CollectionStrategy strategy);
        BencodeConverter elementConverter = options.GetConverter(elementType!);
        Type converterType = typeof(CollectionConverter<,>).MakeGenericType(typeToConvert, elementType!);
        return (BencodeConverter)Activator.CreateInstance(converterType, elementConverter, strategy)!;
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

        if (TryGetQueueLikeInfo(type, out elementType, out strategy))
            return true;

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
    /// Determines whether a type is one of the queue-, stack-, or bag-shaped collections that are materialized through
    /// their own mutation method rather than <see cref="System.Collections.Generic.ICollection{T}" />.
    /// </summary>
    /// <param name="type">The candidate collection type.</param>
    /// <param name="elementType">When this method returns <see langword="true" />, the element type.</param>
    /// <param name="strategy">When this method returns <see langword="true" />, the materialization strategy.</param>
    /// <returns>
    /// <see langword="true" /> when the type is <see cref="Queue{T}" />, <see cref="Stack{T}" />,
    /// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" />,
    /// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" />, or
    /// <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> — or a subclass of one of them — with a public
    /// parameterless constructor; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetQueueLikeInfo(Type type, out Type? elementType, out CollectionStrategy strategy)
    {
        elementType = null;
        strategy = CollectionStrategy.ListAssignable;

        for (Type? current = type; current is not null; current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            Type definition = current.GetGenericTypeDefinition();
            CollectionStrategy? matched = definition == typeof(Queue<>) ? CollectionStrategy.Queue
                : definition == typeof(Stack<>) ? CollectionStrategy.Stack
                : definition == typeof(System.Collections.Concurrent.ConcurrentQueue<>) ? CollectionStrategy.ConcurrentQueue
                : definition == typeof(System.Collections.Concurrent.ConcurrentStack<>) ? CollectionStrategy.ConcurrentStack
                : definition == typeof(System.Collections.Concurrent.ConcurrentBag<>) ? CollectionStrategy.ConcurrentBag
                : null;

            if (matched is null)
                continue;

            if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) is null)
                return false;

            elementType = current.GetGenericArguments()[0];
            strategy = matched.Value;
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
