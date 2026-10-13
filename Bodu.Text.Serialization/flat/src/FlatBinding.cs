// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBinding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides the binding rules the DotEnv and INI serializers share: which dictionaries they bind, which values they
/// leave out when writing, and how they create the instances they read into.
/// </summary>
/// <remarks>
/// Each serializer supplies its own exceptions, so the rules are shared while each package's exception contract is
/// kept.
/// </remarks>
internal static class FlatBinding
{
    /// <summary>The boxed default value of each value type a default test has met.</summary>
    private static readonly ConcurrentDictionary<Type, object> s_defaults = new();

    /// <summary>
    /// Determines whether a type is, or implements, a dictionary keyed by <see cref="string" />.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="valueType">When this method returns <see langword="true" />, the dictionary value type.</param>
    /// <returns><see langword="true" /> when the type is a string-keyed dictionary.</returns>
    internal static bool TryGetStringKeyedDictionary(Type type, [NotNullWhen(true)] out Type? valueType)
    {
        if (IsStringKeyedDictionary(type, out valueType))
            return true;

        foreach (Type candidate in type.GetInterfaces())
        {
            if (IsStringKeyedDictionary(candidate, out valueType))
                return true;
        }

        valueType = null;
        return false;
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
