// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Metadata;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Metadata;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Metadata;
#endif

/// <summary>
/// Builds the <see cref="TypeMetadata" /> for a type by reflecting over its public properties, its public fields (when
/// surfaced by <see cref="FormatOptions.IncludeFields" /> or <see cref="IncludeAttribute" />), and its constructors,
/// applying the serializer's naming policy, attributes, and converter resolution rules.
/// </summary>
internal static class MetadataResolver
{
    /// <summary>The binding flags used to discover serializable instance members.</summary>
    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    /// Resolves the metadata describing how <paramref name="type" /> is mapped under the supplied options.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <param name="options">The serializer options that govern naming and converter resolution.</param>
    /// <returns>The resolved metadata.</returns>
    internal static TypeMetadata Resolve(Type type, FormatOptions options)
    {
        NamingPolicy? namingPolicy = type.GetCustomAttribute<NamingPolicyAttribute>(inherit: false)?.NamingPolicy
            ?? options.PropertyNamingPolicy;

        List<Draft> drafts = [];
        PropertyMetadata? extensionData = null;
        int declarationIndex = 0;
        foreach (PropertyInfo property in type.GetProperties(MemberFlags))
        {
            bool included = property.IsDefined(typeof(IncludeAttribute), inherit: true);
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null || (!property.GetMethod.IsPublic && !included))
                continue;

            IgnoreAttribute? ignore = property.GetCustomAttribute<IgnoreAttribute>(inherit: true);
            if (ignore is not null && ignore.Condition == IgnoreCondition.Always)
                continue;

            FormatConverter converter = ResolveMemberConverter(property, property.PropertyType, options);
            int order = property.GetCustomAttribute<PropertyOrderAttribute>(inherit: true)?.Order ?? 0;
            IgnoreCondition? conditional = ignore?.Condition;
            ObjectCreationHandling? creationHandling = property.GetCustomAttribute<ObjectCreationHandlingAttribute>(inherit: true)?.Handling;
            bool requiredByAttribute = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false)
                || property.IsDefined(typeof(RequiredAttribute), inherit: false);

            if (property.IsDefined(typeof(ExtensionDataAttribute), inherit: true))
            {
#if YAML
                if (extensionData is not null)
                    throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_YamlMultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(property.PropertyType))
                    throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_YamlExtensionDataType, property.Name, type));
#else
                if (extensionData is not null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_MultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(property.PropertyType))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExtensionDataType, property.Name, type));
#endif

                extensionData = new Draft(property, property.Name, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++).ToMetadata();
                continue;
            }

            string wireName = property.GetCustomAttribute<PropertyNameAttribute>(inherit: true)?.Name
                ?? namingPolicy?.ConvertName(property.Name)
                ?? property.Name;

            drafts.Add(new Draft(property, wireName, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++));
        }

        foreach (FieldInfo field in type.GetFields(MemberFlags))
        {
            bool included = field.IsDefined(typeof(IncludeAttribute), inherit: true);
            if (!options.IncludeFields && !included)
                continue;

            IgnoreAttribute? ignore = field.GetCustomAttribute<IgnoreAttribute>(inherit: true);
            if (ignore is not null && ignore.Condition == IgnoreCondition.Always)
                continue;

            FormatConverter converter = ResolveMemberConverter(field, field.FieldType, options);
            int order = field.GetCustomAttribute<PropertyOrderAttribute>(inherit: true)?.Order ?? 0;
            IgnoreCondition? conditional = ignore?.Condition;
            ObjectCreationHandling? creationHandling = field.GetCustomAttribute<ObjectCreationHandlingAttribute>(inherit: true)?.Handling;
            bool requiredByAttribute = field.IsDefined(typeof(RequiredMemberAttribute), inherit: false)
                || field.IsDefined(typeof(RequiredAttribute), inherit: false);

            if (field.IsDefined(typeof(ExtensionDataAttribute), inherit: true))
            {
#if YAML
                if (extensionData is not null)
                    throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_YamlMultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(field.FieldType))
                    throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_YamlExtensionDataType, field.Name, type));
#else
                if (extensionData is not null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_MultipleExtensionData, type));

                if (!IsSupportedExtensionDataType(field.FieldType))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ExtensionDataType, field.Name, type));
#endif

                extensionData = new Draft(field, field.Name, converter, conditional, creationHandling, order, requiredByAttribute, included, declarationIndex++).ToMetadata();
                continue;
            }

            string wireName = field.GetCustomAttribute<PropertyNameAttribute>(inherit: true)?.Name
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
        // canonical document, so the type fails fast here at metadata-resolution time. Members whose wire names differ
        // only by case remain distinct keys on the wire; under a case-insensitive read comparer the later member
        // shadows the earlier one for lookups, which the indexer assignment preserves. The exception type and message
        // shape are pinned per format and intentionally preserved by the format-specific branches below.
#if BENCODE
        Dictionary<string, PropertyMetadata> byOrdinalWireName = new(StringComparer.Ordinal);
        foreach (PropertyMetadata property in ordered)
        {
            if (!byOrdinalWireName.TryAdd(property.WireName, property))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    FormatResourceStrings.Op_Invalid_DuplicateWireName,
                    type,
                    byOrdinalWireName[property.WireName].ClrName,
                    property.ClrName,
                    property.WireName));
            }

            byWireName[property.WireName] = property;
        }
#elif YAML
        HashSet<string> wireNames = new(StringComparer.Ordinal);
        foreach (PropertyMetadata property in ordered)
        {
            if (!wireNames.Add(property.WireName))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_DuplicateWireName, type, property.WireName));

            byWireName[property.WireName] = property;
        }
#else
        HashSet<string> wireNames = new(StringComparer.Ordinal);
        foreach (PropertyMetadata property in ordered)
        {
            if (!wireNames.Add(property.WireName))
                throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_DuplicateWireName, type, property.WireName));

            byWireName[property.WireName] = property;
        }
#endif

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

        UnmappedMemberHandling? unmappedMemberHandling = type.GetCustomAttribute<UnmappedMemberHandlingAttribute>(inherit: false)?.UnmappedMemberHandling;
        ObjectCreationHandling? typeCreationHandling = type.GetCustomAttribute<ObjectCreationHandlingAttribute>(inherit: false)?.Handling;

        return new TypeMetadata(type, ordered, byWireName, constructor, constructorParameters, constructorDefaults, extensionData)
        {
            UnmappedMemberHandling = unmappedMemberHandling,
            CreationHandling = typeCreationHandling,
        };
    }

#if YAML
    /// <summary>
    /// Determines whether a type is a supported extension-data member type. YAML's extension data captures
    /// loosely-typed values rather than DOM nodes, so any type assignable from a string-keyed object dictionary
    /// qualifies.
    /// </summary>
    /// <param name="type">The member type to test.</param>
    /// <returns>
    /// <see langword="true" /> when the type can hold an <c>IDictionary&lt;string, object?&gt;</c>; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool IsSupportedExtensionDataType(Type type) =>
        typeof(IDictionary<string, object?>).IsAssignableFrom(type);
#else
    /// <summary>
    /// Determines whether a type is a supported extension-data member type.
    /// </summary>
    /// <param name="type">The member type to test.</param>
    /// <returns>
    /// <see langword="true" /> when the type is <see cref="FormatObject" />,
    /// <c>IDictionary&lt;string, FormatNode?&gt;</c>, or <c>Dictionary&lt;string, FormatNode?&gt;</c>; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool IsSupportedExtensionDataType(Type type) =>
        type == typeof(FormatObject)
            || type == typeof(IDictionary<string, FormatNode?>)
            || type == typeof(Dictionary<string, FormatNode?>);
#endif

    /// <summary>
    /// Resolves the converter for a member, honoring a member-level converter attribute before falling back to the
    /// options' converter resolution.
    /// </summary>
    /// <param name="member">The property or field whose converter is resolved.</param>
    /// <param name="memberType">The declared type of the member.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converter for the member's value.</returns>
    private static FormatConverter ResolveMemberConverter(MemberInfo member, Type memberType, FormatOptions options)
    {
        ConverterAttribute? attribute = member.GetCustomAttribute<ConverterAttribute>(inherit: true);
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

        ConstructorInfo? attributed = Array.Find(constructors, static c => c.IsDefined(typeof(ConstructorAttribute), inherit: false));
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
        /// Whether the member is opted into binding through non-public accessors by <see cref="IncludeAttribute" />.
        /// </param>
        /// <param name="declarationIndex">The declaration order index.</param>
        internal Draft(
            MemberInfo member,
            string wireName,
            FormatConverter converter,
            IgnoreCondition? conditionalIgnore,
            ObjectCreationHandling? creationHandling,
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
        /// <value>The member.</value>
        internal MemberInfo Member { get; }

        /// <summary>
        /// Gets the resolved wire name.
        /// </summary>
        /// <value>The wire name.</value>
        internal string WireName { get; }

        /// <summary>
        /// Gets the resolved converter.
        /// </summary>
        /// <value>The converter.</value>
        internal FormatConverter Converter { get; }

        /// <summary>
        /// Gets the conditional-ignore setting, or <see langword="null" />.
        /// </summary>
        /// <value>The conditional-ignore setting.</value>
        internal IgnoreCondition? ConditionalIgnore { get; }

        /// <summary>
        /// Gets the member-level object-creation handling, or <see langword="null" />.
        /// </summary>
        /// <value>The member-level object-creation handling.</value>
        internal ObjectCreationHandling? CreationHandling { get; }

        /// <summary>
        /// Gets the write order.
        /// </summary>
        /// <value>The order.</value>
        internal int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the member is marked <see langword="required" />.
        /// </summary>
        /// <value><see langword="true" /> when required by attribute.</value>
        internal bool RequiredByAttribute { get; }

        /// <summary>
        /// Gets a value indicating whether the member is opted into binding through non-public accessors by
        /// <see cref="IncludeAttribute" />.
        /// </summary>
        /// <value><see langword="true" /> when opted in.</value>
        internal bool Included { get; }

        /// <summary>
        /// Gets the declaration order index.
        /// </summary>
        /// <value>The declaration index.</value>
        internal int DeclarationIndex { get; }

        /// <summary>
        /// Gets or sets the bound constructor parameter index, or -1.
        /// </summary>
        /// <value>The constructor parameter index.</value>
        internal int ConstructorParameterIndex { get; set; }

        /// <summary>
        /// Gets or sets the default value supplied when the member binds to an absent constructor parameter.
        /// </summary>
        /// <value>The default value.</value>
        internal object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the member's constructor parameter has no default.
        /// </summary>
        /// <value><see langword="true" /> when required by the constructor.</value>
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
