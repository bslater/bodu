// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bodu.Text.Bencode.Serialization.Metadata;

/// <summary>
/// Builds the <see cref="TypeMetadata" /> for a type by reflecting over its public properties and constructors and
/// applying the serializer's naming policy, attributes, and converter resolution rules.
/// </summary>
internal static class MetadataResolver
{
    /// <summary>
    /// The binding flags used to discover serializable instance properties.
    /// </summary>
    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    /// Resolves the metadata describing how <paramref name="type" /> is mapped under the supplied options.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <param name="options">The serializer options that govern naming and converter resolution.</param>
    /// <returns>The resolved metadata.</returns>
    internal static TypeMetadata Resolve(Type type, BencodeSerializerOptions options)
    {
        List<Draft> drafts = [];
        var declarationIndex = 0;
        foreach (PropertyInfo property in type.GetProperties(MemberFlags))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null || !property.GetMethod.IsPublic)
                continue;

            BencodeIgnoreAttribute? ignore = property.GetCustomAttribute<BencodeIgnoreAttribute>(inherit: true);
            if (ignore is not null && ignore.Condition == BencodeIgnoreCondition.Always)
                continue;

            var wireName = property.GetCustomAttribute<BencodePropertyNameAttribute>(inherit: true)?.Name
                ?? options.PropertyNamingPolicy?.ConvertName(property.Name)
                ?? property.Name;

            BencodeConverter converter = ResolvePropertyConverter(property, options);
            var order = property.GetCustomAttribute<BencodePropertyOrderAttribute>(inherit: true)?.Order ?? 0;
            BencodeIgnoreCondition? conditional = ignore?.Condition;
            var requiredByAttribute = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false);

            drafts.Add(new Draft(property, wireName, converter, conditional, order, requiredByAttribute, declarationIndex++));
        }

        ConstructorInfo? constructor = ChooseConstructor(type);
        ParameterInfo[] parameters = constructor?.GetParameters() ?? [];
        BindConstructorParameters(drafts, parameters);

        PropertyMetadata[] ordered = drafts
            .OrderBy(static draft => draft.Order)
            .ThenBy(static draft => draft.DeclarationIndex)
            .Select(static draft => draft.ToMetadata())
            .ToArray();

        StringComparer comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Dictionary<string, PropertyMetadata> byWireName = new(comparer);
        foreach (PropertyMetadata property in ordered)
            byWireName[property.WireName] = property;

        var constructorParameters = new PropertyMetadata?[parameters.Length];
        var constructorDefaults = new object?[parameters.Length];
        foreach (PropertyMetadata property in ordered)
        {
            if (property.ConstructorParameterIndex >= 0)
            {
                constructorParameters[property.ConstructorParameterIndex] = property;
                constructorDefaults[property.ConstructorParameterIndex] = property.DefaultValue;
            }
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (constructorParameters[i] is null)
                constructorDefaults[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : DefaultOf(parameters[i].ParameterType);
        }

        return new TypeMetadata(type, ordered, byWireName, constructor, constructorParameters, constructorDefaults);
    }

    /// <summary>
    /// Resolves the converter for a property, honoring a member-level converter attribute before falling back to the
    /// options' converter resolution.
    /// </summary>
    /// <param name="property">The property whose converter is resolved.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converter for the property's value.</returns>
    private static BencodeConverter ResolvePropertyConverter(PropertyInfo property, BencodeSerializerOptions options)
    {
        BencodeConverterAttribute? attribute = property.GetCustomAttribute<BencodeConverterAttribute>(inherit: true);
        return attribute is not null
            ? options.InstantiateConverter(attribute.ConverterType, property.PropertyType)
            : options.GetConverter(property.PropertyType);
    }

    /// <summary>
    /// Chooses the constructor used to instantiate the type during deserialization.
    /// </summary>
    /// <param name="type">The type to construct.</param>
    /// <returns>The chosen constructor, or <see langword="null" /> when a parameterless construction applies.</returns>
    private static ConstructorInfo? ChooseConstructor(Type type)
    {
        ConstructorInfo[] constructors = type.GetConstructors(MemberFlags);

        ConstructorInfo? attributed = Array.Find(constructors, static c => c.IsDefined(typeof(BencodeConstructorAttribute), inherit: false));
        if (attributed is not null)
            return attributed;

        ConstructorInfo? parameterless = Array.Find(constructors, static c => c.GetParameters().Length == 0);
        if (parameterless is not null || type.IsValueType)
            return parameterless;

        if (constructors.Length == 0)
            return null;

        ConstructorInfo best = constructors[0];
        foreach (ConstructorInfo candidate in constructors)
        {
            if (candidate.GetParameters().Length > best.GetParameters().Length)
                best = candidate;
        }

        return best;
    }

    /// <summary>
    /// Binds constructor parameters to drafts by matching the parameter name to a member's CLR name,
    /// case-insensitively.
    /// </summary>
    /// <param name="drafts">The property drafts.</param>
    /// <param name="parameters">The chosen constructor's parameters.</param>
    private static void BindConstructorParameters(List<Draft> drafts, ParameterInfo[] parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            Draft? match = drafts.Find(draft => string.Equals(draft.Property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
                continue;

            match.ConstructorParameterIndex = i;
            match.DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : DefaultOf(parameter.ParameterType);
            match.RequiredByConstructor = !parameter.HasDefaultValue;
        }
    }

    /// <summary>
    /// Returns the default value of the specified type as a boxed object.
    /// </summary>
    /// <param name="type">The type whose default value is produced.</param>
    /// <returns>The boxed default value; <see langword="null" /> for reference types.</returns>
    private static object? DefaultOf(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>
    /// Holds the intermediate state for a single member while metadata is resolved, before constructor binding is
    /// known.
    /// </summary>
    private sealed class Draft
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Draft" /> class.
        /// </summary>
        /// <param name="property">The reflected property.</param>
        /// <param name="wireName">The resolved wire name.</param>
        /// <param name="converter">The resolved converter.</param>
        /// <param name="conditionalIgnore">The conditional-ignore setting, or <see langword="null" />.</param>
        /// <param name="order">The write order.</param>
        /// <param name="requiredByAttribute">Whether the member is marked <see langword="required" />.</param>
        /// <param name="declarationIndex">The declaration order index.</param>
        internal Draft(
            PropertyInfo property,
            string wireName,
            BencodeConverter converter,
            BencodeIgnoreCondition? conditionalIgnore,
            int order,
            bool requiredByAttribute,
            int declarationIndex)
        {
            Property = property;
            WireName = wireName;
            Converter = converter;
            ConditionalIgnore = conditionalIgnore;
            Order = order;
            RequiredByAttribute = requiredByAttribute;
            DeclarationIndex = declarationIndex;
            ConstructorParameterIndex = -1;
        }

        /// <summary>
        /// Gets the reflected property.
        /// </summary>
        /// <returns>The property.</returns>
        internal PropertyInfo Property { get; }

        /// <summary>
        /// Gets the resolved wire name.
        /// </summary>
        /// <returns>The wire name.</returns>
        internal string WireName { get; }

        /// <summary>
        /// Gets the resolved converter.
        /// </summary>
        /// <returns>The converter.</returns>
        internal BencodeConverter Converter { get; }

        /// <summary>
        /// Gets the conditional-ignore setting, or <see langword="null" />.
        /// </summary>
        /// <returns>The conditional-ignore setting.</returns>
        internal BencodeIgnoreCondition? ConditionalIgnore { get; }

        /// <summary>
        /// Gets the write order.
        /// </summary>
        /// <returns>The order.</returns>
        internal int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the member is marked <see langword="required" />.
        /// </summary>
        /// <returns><see langword="true" /> when required by attribute.</returns>
        internal bool RequiredByAttribute { get; }

        /// <summary>
        /// Gets the declaration order index.
        /// </summary>
        /// <returns>The declaration index.</returns>
        internal int DeclarationIndex { get; }

        /// <summary>
        /// Gets or sets the bound constructor parameter index, or -1.
        /// </summary>
        /// <returns>The constructor parameter index.</returns>
        internal int ConstructorParameterIndex { get; set; }

        /// <summary>
        /// Gets or sets the default value supplied when the member binds to an absent constructor parameter.
        /// </summary>
        /// <returns>The default value.</returns>
        internal object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the member's constructor parameter has no default.
        /// </summary>
        /// <returns><see langword="true" /> when required by the constructor.</returns>
        internal bool RequiredByConstructor { get; set; }

        /// <summary>
        /// Produces the immutable <see cref="PropertyMetadata" /> from this draft.
        /// </summary>
        /// <returns>The resolved member metadata.</returns>
        internal PropertyMetadata ToMetadata() =>
            new(
                Property,
                WireName,
                Converter,
                ConditionalIgnore,
                Order,
                ConstructorParameterIndex,
                RequiredByAttribute || RequiredByConstructor,
                DefaultValue);
    }
}
