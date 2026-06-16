// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bodu.Text.Bencode.Nodes;

namespace Bodu.Text.Bencode.Serialization.Metadata;

/// <summary>
/// Builds the <see cref="TypeMetadata" /> for a type by reflecting over its public properties, its public fields (when
/// surfaced by <see cref="BencodeSerializerOptions.IncludeFields" /> or <see cref="BencodeIncludeAttribute" />), and
/// its constructors, applying the serializer's naming policy, attributes, and converter resolution rules.
/// </summary>
internal static class MetadataResolver
{
    /// <summary>
    /// The binding flags used to discover serializable instance members.
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
        BencodeNamingPolicy? namingPolicy = type.GetCustomAttribute<BencodeNamingPolicyAttribute>(inherit: false)?.NamingPolicy
            ?? options.PropertyNamingPolicy;

        List<Draft> drafts = [];
        PropertyMetadata? extensionData = null;
        int declarationIndex = 0;
        foreach (PropertyInfo property in type.GetProperties(MemberFlags))
        {
            bool included = property.IsDefined(typeof(BencodeIncludeAttribute), inherit: true);
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null || (!property.GetMethod.IsPublic && !included))
                continue;

            BencodeIgnoreAttribute? ignore = property.GetCustomAttribute<BencodeIgnoreAttribute>(inherit: true);
            if (ignore is not null && ignore.Condition == BencodeIgnoreCondition.Always)
                continue;

            BencodeConverter converter = ResolveMemberConverter(property, property.PropertyType, options);
            int order = property.GetCustomAttribute<BencodePropertyOrderAttribute>(inherit: true)?.Order ?? 0;
            BencodeIgnoreCondition? conditional = ignore?.Condition;
            BencodeObjectCreationHandling? creationHandling = property.GetCustomAttribute<BencodeObjectCreationHandlingAttribute>(inherit: true)?.Handling;
            bool requiredByAttribute = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false)
                || property.IsDefined(typeof(BencodeRequiredAttribute), inherit: false);

            if (property.IsDefined(typeof(BencodeExtensionDataAttribute), inherit: true))
            {
                if (extensionData is not null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_MultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(property.PropertyType))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExtensionDataType, property.Name, type));

                extensionData = new Draft(property, property.Name, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++).ToMetadata();
                continue;
            }

            string wireName = property.GetCustomAttribute<BencodePropertyNameAttribute>(inherit: true)?.Name
                ?? namingPolicy?.ConvertName(property.Name)
                ?? property.Name;

            drafts.Add(new Draft(property, wireName, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++));
        }

        foreach (FieldInfo field in type.GetFields(MemberFlags))
        {
            bool included = field.IsDefined(typeof(BencodeIncludeAttribute), inherit: true);
            if (!options.IncludeFields && !included)
                continue;

            BencodeIgnoreAttribute? ignore = field.GetCustomAttribute<BencodeIgnoreAttribute>(inherit: true);
            if (ignore is not null && ignore.Condition == BencodeIgnoreCondition.Always)
                continue;

            BencodeConverter converter = ResolveMemberConverter(field, field.FieldType, options);
            int order = field.GetCustomAttribute<BencodePropertyOrderAttribute>(inherit: true)?.Order ?? 0;
            BencodeIgnoreCondition? conditional = ignore?.Condition;
            BencodeObjectCreationHandling? creationHandling = field.GetCustomAttribute<BencodeObjectCreationHandlingAttribute>(inherit: true)?.Handling;
            bool requiredByAttribute = field.IsDefined(typeof(RequiredMemberAttribute), inherit: false)
                || field.IsDefined(typeof(BencodeRequiredAttribute), inherit: false);

            if (field.IsDefined(typeof(BencodeExtensionDataAttribute), inherit: true))
            {
                if (extensionData is not null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_MultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(field.FieldType))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExtensionDataType, field.Name, type));

                extensionData = new Draft(field, field.Name, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++).ToMetadata();
                continue;
            }

            string wireName = field.GetCustomAttribute<BencodePropertyNameAttribute>(inherit: true)?.Name
                ?? namingPolicy?.ConvertName(field.Name)
                ?? field.Name;

            drafts.Add(new Draft(field, wireName, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++));
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

        // Collision detection is always ordinal: two members on the same exact wire key can never be written as a
        // canonical dictionary, so the type fails fast here at metadata-resolution time.
        // Members whose wire names differ only by case remain distinct keys on the wire; under a case-insensitive
        // read comparer the later member shadows the earlier one for lookups, which the indexer assignment preserves.
        Dictionary<string, PropertyMetadata> byOrdinalWireName = new(StringComparer.Ordinal);
        foreach (PropertyMetadata property in ordered)
        {
            if (!byOrdinalWireName.TryAdd(property.WireName, property))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    BencodeResourceStrings.Op_Invalid_DuplicateWireName,
                    type,
                    byOrdinalWireName[property.WireName].ClrName,
                    property.ClrName,
                    property.WireName));
            }

            byWireName[property.WireName] = property;
        }

        var constructorParameters = new PropertyMetadata?[parameters.Length];
        object?[] constructorDefaults = new object?[parameters.Length];
        foreach (PropertyMetadata property in ordered)
        {
            if (property.ConstructorParameterIndex >= 0)
            {
                constructorParameters[property.ConstructorParameterIndex] = property;
                constructorDefaults[property.ConstructorParameterIndex] = property.DefaultValue;
            }
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            if (constructorParameters[i] is null)
                constructorDefaults[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : DefaultOf(parameters[i].ParameterType);
        }

        BencodeUnmappedMemberHandling? unmappedMemberHandling = type.GetCustomAttribute<BencodeUnmappedMemberHandlingAttribute>(inherit: false)?.UnmappedMemberHandling;
        BencodeObjectCreationHandling? typeCreationHandling = type.GetCustomAttribute<BencodeObjectCreationHandlingAttribute>(inherit: false)?.Handling;

        return new TypeMetadata(type, ordered, byWireName, constructor, constructorParameters, constructorDefaults, extensionData)
        {
            UnmappedMemberHandling = unmappedMemberHandling,
            CreationHandling = typeCreationHandling,
        };
    }

    /// <summary>
    /// Determines whether a type is a supported extension-data member type.
    /// </summary>
    /// <param name="type">The member type to test.</param>
    /// <returns>
    /// <see langword="true" /> when the type is <see cref="BencodeObject" />,
    /// <c>IDictionary&lt;string, BencodeNode?&gt;</c>, or <c>Dictionary&lt;string, BencodeNode?&gt;</c>; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool IsSupportedExtensionDataType(Type type) =>
        type == typeof(BencodeObject)
            || type == typeof(IDictionary<string, BencodeNode?>)
            || type == typeof(Dictionary<string, BencodeNode?>);

    /// <summary>
    /// Resolves the converter for a member, honoring a member-level converter attribute before falling back to the
    /// options' converter resolution.
    /// </summary>
    /// <param name="member">The property or field whose converter is resolved.</param>
    /// <param name="memberType">The declared type of the member.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converter for the member's value.</returns>
    private static BencodeConverter ResolveMemberConverter(MemberInfo member, Type memberType, BencodeSerializerOptions options)
    {
        BencodeConverterAttribute? attribute = member.GetCustomAttribute<BencodeConverterAttribute>(inherit: true);
        return attribute is not null
            ? options.InstantiateConverter(attribute.ConverterType, memberType)
            : options.GetConverter(memberType);
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
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            Draft? match = drafts.Find(draft => string.Equals(draft.Member.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
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
        /// <param name="member">The reflected property or field.</param>
        /// <param name="wireName">The resolved wire name.</param>
        /// <param name="converter">The resolved converter.</param>
        /// <param name="conditionalIgnore">The conditional-ignore setting, or <see langword="null" />.</param>
        /// <param name="creationHandling">
        /// The member-level object-creation handling, or <see langword="null" />.
        /// </param>
        /// <param name="order">The write order.</param>
        /// <param name="requiredByAttribute">Whether the member is marked <see langword="required" />.</param>
        /// <param name="included">
        /// Whether the member is opted into binding through non-public accessors by
        /// <see cref="BencodeIncludeAttribute" />.
        /// </param>
        /// <param name="declarationIndex">The declaration order index.</param>
        internal Draft(
            MemberInfo member,
            string wireName,
            BencodeConverter converter,
            BencodeIgnoreCondition? conditionalIgnore,
            BencodeObjectCreationHandling? creationHandling,
            int order,
            bool requiredByAttribute,
            bool included,
            int declarationIndex)
        {
            Member = member;
            WireName = wireName;
            Converter = converter;
            ConditionalIgnore = conditionalIgnore;
            CreationHandling = creationHandling;
            Order = order;
            RequiredByAttribute = requiredByAttribute;
            Included = included;
            DeclarationIndex = declarationIndex;
            ConstructorParameterIndex = -1;
        }

        /// <summary>
        /// Gets the reflected property or field.
        /// </summary>
        /// <returns>The member.</returns>
        internal MemberInfo Member { get; }

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
        /// Gets the member-level object-creation handling, or <see langword="null" />.
        /// </summary>
        /// <returns>The member-level object-creation handling.</returns>
        internal BencodeObjectCreationHandling? CreationHandling { get; }

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
        /// Gets a value indicating whether the member is opted into binding through non-public accessors by
        /// <see cref="BencodeIncludeAttribute" />.
        /// </summary>
        /// <returns><see langword="true" /> when opted in.</returns>
        internal bool Included { get; }

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
                Member,
                WireName,
                Converter,
                ConditionalIgnore,
                Order,
                ConstructorParameterIndex,
                RequiredByAttribute || RequiredByConstructor,
                DefaultValue,
                Included)
            {
                CreationHandling = CreationHandling,
            };
    }
}
