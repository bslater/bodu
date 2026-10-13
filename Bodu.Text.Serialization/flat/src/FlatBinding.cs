// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBinding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides the binding rules the DotEnv and INI serializers share: which dictionaries they bind, which values they
/// leave out when writing, and how they create the instances they read into.
/// </summary>
/// <remarks>
/// <para>
/// Each serializer supplies its own exceptions, so the rules are shared while each package's exception contract is
/// kept.
/// </para>
/// <para>
/// A string-keyed dictionary is a type that implements <see cref="IDictionary{TKey, TValue}" /> keyed by
/// <see cref="string" />, whether or not it also implements the non-generic <see cref="IDictionary" />, the rule
/// <c>System.Text.Json</c> follows. Its entries are read and written through <see cref="IDictionary" /> when the type
/// implements it, and otherwise through the generic interface, so a type that implements only the generic interface is
/// written and read alike.
/// </para>
/// <para>
/// A dictionary whose type is not keyed by <see cref="string" />, such as a <see cref="Hashtable" />, a
/// <see cref="SortedList" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />, is not one, even
/// when the keys it holds are strings: its keys could not be read back from a document as the type they are, so the
/// serializers reject it as they reject any other collection.
/// </para>
/// </remarks>
internal static class FlatBinding
{
    /// <summary>The boxed default value of each value type a default test has met.</summary>
    private static readonly ConcurrentDictionary<Type, object> s_defaults = new();

    /// <summary>The dictionary value type of each type met that is a string-keyed dictionary, or <see langword="null" /> for each type met that is not one.</summary>
    private static readonly ConcurrentDictionary<Type, Type?> s_valueTypes = new();

    /// <summary>The entry copier, by value type, of the dictionaries that are not an <see cref="IDictionary" />.</summary>
    private static readonly ConcurrentDictionary<Type, Func<object, OrderedDictionary>> s_entryCopiers = new();

    /// <summary>The entry setter, by value type, of the dictionaries that are not an <see cref="IDictionary" />.</summary>
    private static readonly ConcurrentDictionary<Type, Action<object, string, object?>> s_entrySetters = new();

    /// <summary>The generic definition of <see cref="CopyEntries{TValue}(object)" />.</summary>
    private static readonly MethodInfo s_copyEntries =
        ((Func<object, OrderedDictionary>)CopyEntries<object>).Method.GetGenericMethodDefinition();

    /// <summary>The generic definition of <see cref="SetGenericEntry{TValue}(object, string, object?)" />.</summary>
    private static readonly MethodInfo s_setGenericEntry =
        ((Action<object, string, object?>)SetGenericEntry<object>).Method.GetGenericMethodDefinition();

    /// <summary>
    /// Determines whether a type is, or implements, a dictionary keyed by <see cref="string" />.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is a string-keyed dictionary.</returns>
    /// <remarks>
    /// The answer for each type is found once and kept, so a test allocates nothing once its type has been met.
    /// </remarks>
    internal static bool TryGetStringKeyedDictionary(Type type, [NotNullWhen(true)] out Type? valueType)
    {
        valueType = s_valueTypes.GetOrAdd(type, static type => FindStringKeyedValueType(type));
        return valueType is not null;
    }

    /// <summary>
    /// Determines whether a value is a dictionary the serializers write as entries, and gets the entries to write.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="dictionary">
    /// When this method returns <see langword="true" />, the value itself when it implements <see cref="IDictionary" />,
    /// or else a copy of its entries, in the order it enumerates them.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the value's type implements <see cref="IDictionary{TKey, TValue}" /> keyed by
    /// <see cref="string" />; <see langword="false" /> for any other value, a dictionary with other keys included.
    /// </returns>
    /// <remarks>
    /// A value that implements only the generic interface has its entries copied through it, so the serializers write
    /// it as they write a <see cref="Dictionary{TKey, TValue}" />. A <see cref="Hashtable" />, a
    /// <see cref="SortedList" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" /> is not keyed
    /// by <see cref="string" />, so it is not written as entries even when the keys it holds are strings.
    /// </remarks>
    [RequiresDynamicCode("Reading a dictionary through its generic interface makes a generic method at run time.")]
    [RequiresUnreferencedCode("Reading a dictionary through its generic interface makes a generic method that trimming cannot statically determine.")]
    internal static bool TryGetDictionary([NotNullWhen(true)] object? value, [NotNullWhen(true)] out IDictionary? dictionary) =>
        TryGetDictionary(value, out dictionary, out _);

    /// <summary>
    /// Determines whether a value is a dictionary the serializers write as entries, and gets the entries to write and
    /// the dictionary's value type.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="dictionary">
    /// When this method returns <see langword="true" />, the value itself when it implements <see cref="IDictionary" />,
    /// or else a copy of its entries, in the order it enumerates them.
    /// </param>
    /// <param name="valueType">
    /// When this method returns <see langword="true" />, the value type of the <see cref="IDictionary{TKey, TValue}" />
    /// keyed by <see cref="string" /> that the value implements.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the value's type implements <see cref="IDictionary{TKey, TValue}" /> keyed by
    /// <see cref="string" />; <see langword="false" /> for any other value, a dictionary with other keys included.
    /// </returns>
    [RequiresDynamicCode("Reading a dictionary through its generic interface makes a generic method at run time.")]
    [RequiresUnreferencedCode("Reading a dictionary through its generic interface makes a generic method that trimming cannot statically determine.")]
    internal static bool TryGetDictionary(
        [NotNullWhen(true)] object? value,
        [NotNullWhen(true)] out IDictionary? dictionary,
        [NotNullWhen(true)] out Type? valueType)
    {
        // Only an enumerable can implement a dictionary interface, so a scalar, the most common value, never has its
        // type looked up.
        if (value is IEnumerable and not string && TryGetStringKeyedDictionary(value.GetType(), out valueType))
        {
            dictionary = value as IDictionary ?? GetEntryCopier(valueType)(value);
            return true;
        }

        dictionary = null;
        valueType = null;
        return false;
    }

    /// <summary>
    /// Stores an entry in a string-keyed dictionary created for reading, through <see cref="IDictionary" /> when the
    /// dictionary implements it, or else through the <see cref="IDictionary{TKey, TValue}" /> it implements.
    /// </summary>
    /// <param name="dictionary">The dictionary, which implements <see cref="IDictionary{TKey, TValue}" />.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, of <paramref name="valueType" /> or <see langword="null" />.</param>
    [RequiresDynamicCode("Writing a dictionary through its generic interface makes a generic method at run time.")]
    [RequiresUnreferencedCode("Writing a dictionary through its generic interface makes a generic method that trimming cannot statically determine.")]
    internal static void SetEntry(object dictionary, Type valueType, string key, object? value)
    {
        if (dictionary is IDictionary nonGeneric)
        {
            nonGeneric[key] = value;
            return;
        }

        Action<object, string, object?> setEntry = s_entrySetters.GetOrAdd(
            valueType,
            static valueType => s_setGenericEntry.MakeGenericMethod(valueType).CreateDelegate<Action<object, string, object?>>());
        setEntry(dictionary, key, value);
    }

    /// <summary>
    /// Resolves the type to create for a dictionary of a declared type: the type itself, or
    /// <see cref="Dictionary{TKey, TValue}" /> when it is an interface.
    /// </summary>
    /// <param name="declaredType">The declared dictionary type, possibly an interface.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>The type to create.</returns>
    [RequiresDynamicCode("Creating a dictionary for an interface makes a generic type at run time.")]
    internal static Type ConcreteDictionaryType(Type declaredType, Type valueType) =>
        declaredType.IsInterface
            ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
            : declaredType;

    /// <summary>
    /// Creates an instance of a type that a document is read into, after checking that the type can be created.
    /// </summary>
    /// <param name="type">The type to create.</param>
    /// <param name="createNotCreatableException">
    /// Creates the serializer's exception for a type that cannot be created.
    /// </param>
    /// <returns>The new instance.</returns>
    /// <exception cref="Exception">
    /// The exception <paramref name="createNotCreatableException" /> creates, when <paramref name="type" /> is an
    /// interface or an abstract class, or a class with no public parameterless constructor.
    /// </exception>
    [RequiresUnreferencedCode("Creating an instance needs a public parameterless constructor that trimming cannot statically determine.")]
    internal static object CreateInstance(Type type, Func<Type, Exception> createNotCreatableException)
    {
        // A value type can always be created; a reference type needs the constructor Activator calls.
        if (!type.IsValueType && (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) is null))
            throw createNotCreatableException(type);

        return Activator.CreateInstance(type)!;
    }

    /// <summary>
    /// Determines whether a member's value is left out of the output under its own ignore condition or, without one,
    /// the options' default.
    /// </summary>
    /// <param name="value">The member's value.</param>
    /// <param name="memberCondition">The member's own ignore condition, or <see langword="null" />.</param>
    /// <param name="defaultCondition">The options' default ignore condition.</param>
    /// <returns><see langword="true" /> when the member is left out.</returns>
    internal static bool ShouldSkipOnWrite(object? value, IgnoreCondition? memberCondition, IgnoreCondition defaultCondition) =>
        (memberCondition ?? defaultCondition) switch
        {
            IgnoreCondition.Always => true,
            IgnoreCondition.WhenWritingNull => value is null,
            IgnoreCondition.WhenWritingDefault => value is null || IsDefaultValue(value),
            _ => false,
        };

    /// <summary>
    /// Determines whether a dictionary entry's value is left out of the output under the options' default ignore
    /// condition.
    /// </summary>
    /// <param name="value">The entry's value.</param>
    /// <param name="defaultCondition">The options' default ignore condition.</param>
    /// <returns>
    /// <see langword="true" /> when the value is <see langword="null" /> and the condition leaves out null or default
    /// values.
    /// </returns>
    internal static bool ShouldSkipDictionaryValue(object? value, IgnoreCondition defaultCondition) =>
        value is null && defaultCondition is IgnoreCondition.WhenWritingNull or IgnoreCondition.WhenWritingDefault;

    /// <summary>
    /// Determines whether a value equals the default of its type.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when the value is a value type's default.</returns>
    /// <remarks>
    /// The default of each value type is boxed once and kept, so a test allocates nothing once its type has been met.
    /// </remarks>
    private static bool IsDefaultValue(object value)
    {
        Type type = value.GetType();
        if (!type.IsValueType)
            return false;

        object defaultValue = s_defaults.GetOrAdd(type, static valueType => Activator.CreateInstance(valueType)!);
        return value.Equals(defaultValue);
    }

    /// <summary>
    /// Finds the value type of the <see cref="IDictionary{TKey, TValue}" /> keyed by <see cref="string" /> that a type
    /// is or implements.
    /// </summary>
    /// <param name="type">The type to search.</param>
    /// <returns>
    /// The dictionary value type, or <see langword="null" /> when the type is not a string-keyed dictionary.
    /// </returns>
    private static Type? FindStringKeyedValueType(Type type)
    {
        if (IsStringKeyedDictionary(type, out Type? valueType))
            return valueType;

        foreach (Type candidate in type.GetInterfaces())
        {
            if (IsStringKeyedDictionary(candidate, out valueType))
                return valueType;
        }

        return null;
    }

    /// <summary>
    /// Gets the entry copier of the string-keyed dictionaries of a value type that are not an
    /// <see cref="IDictionary" />.
    /// </summary>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>The copier.</returns>
    [RequiresDynamicCode("Reading a dictionary through its generic interface makes a generic method at run time.")]
    [RequiresUnreferencedCode("Reading a dictionary through its generic interface makes a generic method that trimming cannot statically determine.")]
    private static Func<object, OrderedDictionary> GetEntryCopier(Type valueType) =>
        s_entryCopiers.GetOrAdd(
            valueType,
            static valueType => s_copyEntries.MakeGenericMethod(valueType).CreateDelegate<Func<object, OrderedDictionary>>());

    /// <summary>
    /// Copies the entries of a dictionary that implements <see cref="IDictionary{TKey, TValue}" /> keyed by
    /// <see cref="string" /> into a non-generic dictionary, in the order it enumerates them.
    /// </summary>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="dictionary">The dictionary to copy.</param>
    /// <returns>The copy.</returns>
    private static OrderedDictionary CopyEntries<TValue>(object dictionary)
    {
        var source = (IDictionary<string, TValue>)dictionary;
        var copy = new OrderedDictionary(source.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, TValue> entry in source)
            copy[entry.Key] = entry.Value;

        return copy;
    }

    /// <summary>
    /// Stores an entry in a dictionary through the <see cref="IDictionary{TKey, TValue}" /> keyed by
    /// <see cref="string" /> that it implements.
    /// </summary>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, of <typeparamref name="TValue" /> or <see langword="null" />.</param>
    private static void SetGenericEntry<TValue>(object dictionary, string key, object? value) =>
        ((IDictionary<string, TValue>)dictionary)[key] = (TValue)value!;

    /// <summary>
    /// Determines whether a type is the <see cref="IDictionary{TKey, TValue}" /> interface keyed by
    /// <see cref="string" />.
    /// </summary>
    /// <param name="candidate">The type to test.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is that interface.</returns>
    private static bool IsStringKeyedDictionary(Type candidate, [NotNullWhen(true)] out Type? valueType)
    {
        if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            Type[] args = candidate.GetGenericArguments();
            if (args[0] == typeof(string))
            {
                valueType = args[1];
                return true;
            }
        }

        valueType = null;
        return false;
    }
}
