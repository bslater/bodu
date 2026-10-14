// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Emits reflection-free serializer factories for annotated format POCOs: an
/// <c>IDelimitedRecordFactory&lt;TRecord&gt;</c> for each <c>[DelimitedRecord]</c> type and an
/// <c>IIniSectionFactory&lt;TSection&gt;</c> for each <c>[IniSection]</c> type, exposed as generated static
/// <c>DelimitedFactory</c> / <c>IniFactory</c> properties on the partial type.
/// </summary>
/// <remarks>
/// <para>
/// The generated factories map the public read/write instance properties declared on the type in declaration order,
/// honor <c>[PropertyName]</c> for wire names, skip members annotated <c>[Ignore]</c> (with
/// <c>IgnoreCondition.Always</c>), and convert scalars with the invariant culture, mirroring the runtime reflection
/// binders for the members both map. They differ from those binders where the binders read the options or reflect over
/// more: the naming policy, inherited, get-only and <c>init</c>-only properties, <c>[PropertyOrder]</c>, case-sensitive
/// name matching, and <c>[Required]</c>. Their scalar set is the binders'.
/// </para>
/// <para>
/// A public instance property whose type is not a supported scalar reports <c>BTFG002</c>, an error, and no factory is
/// generated for its type: the reflection binders reject such a type rather than leave the member out. Every property
/// the binders map is tested, inherited, get-only and <c>init</c>-only ones included, although the factory maps only
/// the read/write properties declared on the type. A property declared as <see cref="object" /> is refused too,
/// although the Delimited binder accepts one that holds a scalar, because a factory maps only the scalar set.
/// <c>[Ignore]</c> leaves such a member out deliberately.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class FormatFactoryGenerator : IIncrementalGenerator
{
    /// <summary>The fully qualified metadata name of the delimited record attribute.</summary>
    private const string DelimitedAttributeName = "Bodu.Text.Delimited.DelimitedRecordAttribute";

    /// <summary>The fully qualified metadata name of the INI section attribute.</summary>
    private const string IniAttributeName = "Bodu.Text.Ini.IniSectionAttribute";

    /// <summary>The fully qualified name of the shared property-name attribute.</summary>
    private const string PropertyNameAttributeName = "Bodu.Text.Serialization.PropertyNameAttribute";

    /// <summary>The fully qualified name of the shared ignore attribute.</summary>
    private const string IgnoreAttributeName = "Bodu.Text.Serialization.IgnoreAttribute";

    /// <summary>The numeric value of <c>IgnoreCondition.Always</c>.</summary>
    private const int IgnoreConditionAlways = 1;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterFactoryPipeline(context, DelimitedAttributeName, FactoryKind.Delimited);
        RegisterFactoryPipeline(context, IniAttributeName, FactoryKind.Ini);
    }

    /// <summary>
    /// Registers the incremental pipeline for one marker attribute.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    /// <param name="attributeMetadataName">The marker attribute's fully qualified metadata name.</param>
    /// <param name="kind">The factory kind the attribute requests.</param>
    private static void RegisterFactoryPipeline(IncrementalGeneratorInitializationContext context, string attributeMetadataName, FactoryKind kind)
    {
        IncrementalValuesProvider<FactoryModel> models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                attributeMetadataName,
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: (ctx, _) => CreateModel(ctx, kind))
            .Where(static model => model is not null)!;

        context.RegisterSourceOutput(models, static (productionContext, model) => Emit(productionContext, model));
    }

    /// <summary>
    /// Builds the equatable factory model for one annotated type.
    /// </summary>
    /// <param name="context">The attribute syntax context.</param>
    /// <param name="kind">The factory kind being generated.</param>
    /// <returns>The model, or <see langword="null" /> when the target is not a named type.</returns>
    private static FactoryModel? CreateModel(GeneratorAttributeSyntaxContext context, FactoryKind kind)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
            return null;

        bool isPartial = true;
        bool isGeneric = false;
        var containingHeaders = new List<string>();

        for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
        {
            isPartial &= IsDeclaredPartial(current);
            isGeneric |= current.Arity > 0;

            if (!SymbolEqualityComparer.Default.Equals(current, symbol))
                containingHeaders.Insert(0, BuildDeclarationHeader(current));
        }

        var members = new List<MemberModel>();

        foreach (ISymbol candidate in symbol.GetMembers())
        {
            if (candidate is not IPropertySymbol property ||
                property.IsStatic ||
                property.IsIndexer ||
                property.DeclaredAccessibility != Accessibility.Public ||
                property.GetMethod is not { DeclaredAccessibility: Accessibility.Public } ||
                property.SetMethod is not { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false })
            {
                continue;
            }

            if (IsIgnored(property))
                continue;

            // A property of an unsupported type is not mapped; FindUnsupportedMembers decides whether it refuses the type.
            MemberModel? member = CreateMemberModel(property);
            if (member is not null)
                members.Add(member);
        }

        return new FactoryModel(
            Kind: kind,
            Namespace: symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString(),
            TypeName: symbol.Name,
            ContainingTypes: new EquatableArray<string>([.. containingHeaders]),
            DeclarationHeader: BuildDeclarationHeader(symbol),
            Accessibility: "public",
            IsPartial: isPartial,
            IsGeneric: isGeneric,
            Members: new EquatableArray<MemberModel>([.. members]),
            UnsupportedMembers: new EquatableArray<string>([.. FindUnsupportedMembers(symbol)]));
    }

    /// <summary>
    /// Finds the members the reflection binders map and reject: every public instance property of the type, inherited
    /// ones included and whatever its accessors, that <c>[Ignore]</c> does not leave out and whose type is not a
    /// supported scalar.
    /// </summary>
    /// <param name="symbol">The annotated type.</param>
    /// <returns>The name and type display of each such member, the type's own declarations first.</returns>
    /// <remarks>
    /// The binders map each name once, to its most derived public declaration, so a base property hidden by a public
    /// property of the same name is not tested, and an <c>[Ignore]</c> on that declaration, or on a property it
    /// overrides, leaves the name out. A property the factory does not map, because it is get-only, <c>init</c>-only or
    /// inherited, is tested all the same, since the binders reject its type when they write or read the type.
    /// </remarks>
    private static List<string> FindUnsupportedMembers(INamedTypeSymbol symbol)
    {
        var unsupported = new List<string>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
        {
            foreach (ISymbol candidate in current.GetMembers())
            {
                if (candidate is not IPropertySymbol { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public } property)
                    continue;

                // A declaration of the same name in a more derived type hides this one, which the binders do not map.
                if (!names.Add(property.Name))
                    continue;

                if (!IsIgnoredThroughOverrides(property) && !IsSupportedScalar(property.Type))
                    unsupported.Add($"{property.Name}:{property.Type.ToDisplayString()}");
            }
        }

        return unsupported;
    }

    /// <summary>
    /// Reports the model's diagnostics and, when generation is possible, adds the factory source.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="model">The factory model.</param>
    private static void Emit(SourceProductionContext context, FactoryModel model)
    {
        string attributeShortName = model.Kind == FactoryKind.Delimited ? "DelimitedRecord" : "IniSection";
        string qualifiedName = model.Namespace.Length == 0 ? model.TypeName : $"{model.Namespace}.{model.TypeName}";

        if (!model.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.TypeMustBePartial, Location.None, qualifiedName, attributeShortName));
            return;
        }

        if (model.IsGeneric)
        {
            context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.TypeMustNotBeGeneric, Location.None, qualifiedName, attributeShortName));
            return;
        }

        // The type gets no factory rather than one that silently leaves a member out, and so reads and writes a type
        // the reflection binders reject; [Ignore] is the way to leave a member out deliberately.
        foreach (string unsupportedMember in model.UnsupportedMembers)
        {
            int separator = unsupportedMember.IndexOf(':');
            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.MemberTypeUnsupported,
                Location.None,
                unsupportedMember.Substring(0, separator),
                qualifiedName,
                unsupportedMember.Substring(separator + 1)));
        }

        if (model.UnsupportedMembers.Count > 0)
            return;

        string hintName = $"{qualifiedName}.{model.Kind}.g.cs";
        context.AddSource(hintName, FactoryEmitter.Emit(model));
    }

    /// <summary>
    /// Determines whether every declaration of the type carries the <see langword="partial" /> modifier.
    /// </summary>
    /// <param name="symbol">The type symbol.</param>
    /// <returns><see langword="true" /> when the type is declared partial.</returns>
    private static bool IsDeclaredPartial(INamedTypeSymbol symbol)
    {
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the partial declaration header for a type (for example <c>partial class Person</c>), omitting
    /// accessibility so the generated part never conflicts with the authored one.
    /// </summary>
    /// <param name="symbol">The type symbol.</param>
    /// <returns>The declaration header text.</returns>
    private static string BuildDeclarationHeader(INamedTypeSymbol symbol)
    {
        string keyword = (symbol.IsRecord, symbol.TypeKind) switch
        {
            (true, TypeKind.Struct) => "record struct",
            (true, _) => "record",
            (false, TypeKind.Struct) => "struct",
            _ => "class",
        };

        return $"partial {keyword} {symbol.Name}";
    }

    /// <summary>
    /// Determines whether the property is excluded by an unconditional <c>[Ignore]</c> annotation.
    /// </summary>
    /// <param name="property">The property symbol.</param>
    /// <returns><see langword="true" /> when the property must be skipped.</returns>
    private static bool IsIgnored(IPropertySymbol property)
    {
        foreach (AttributeData attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != IgnoreAttributeName)
                continue;

            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == "Condition")
                    return argument.Value.Value is int condition && condition == IgnoreConditionAlways;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the property, or the nearest property it overrides that carries an <c>[Ignore]</c>
    /// annotation, is excluded by an unconditional one, as the reflection binders read the inherited attribute.
    /// </summary>
    /// <param name="property">The property symbol.</param>
    /// <returns><see langword="true" /> when the reflection binders leave the property out.</returns>
    private static bool IsIgnoredThroughOverrides(IPropertySymbol property)
    {
        for (IPropertySymbol? current = property; current is not null; current = current.OverriddenProperty)
        {
            if (ReadIgnoreCondition(current) is int condition)
                return condition == IgnoreConditionAlways;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a property type, or the type it makes nullable, is a scalar the factories convert.
    /// </summary>
    /// <param name="type">The property type.</param>
    /// <returns>
    /// <see langword="true" /> when the type is a supported scalar or its <see cref="Nullable{T}" />.
    /// </returns>
    private static bool IsSupportedScalar(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            type = nullable.TypeArguments[0];

        return ResolveScalarKind(type) != ScalarKind.Unsupported;
    }

    /// <summary>
    /// Builds the member model for a mapped property, resolving its wire name and scalar conversion.
    /// </summary>
    /// <param name="property">The property symbol.</param>
    /// <returns>The member model, or <see langword="null" /> when the property type is unsupported.</returns>
    private static MemberModel? CreateMemberModel(IPropertySymbol property)
    {
        string wireName = property.Name;
        foreach (AttributeData attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == PropertyNameAttributeName &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is string explicitName)
            {
                wireName = explicitName;
            }
        }

        ITypeSymbol effective = property.Type;
        bool isNullable = false;

        if (effective is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            effective = nullable.TypeArguments[0];
            isNullable = true;
        }

        ScalarKind scalar = ResolveScalarKind(effective);
        if (scalar == ScalarKind.Unsupported)
            return null;

        return new MemberModel(
            PropertyName: property.Name,
            WireName: wireName,
            Scalar: scalar,
            IsNullable: isNullable,
            TypeDisplay: effective.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IgnoreCondition: ReadIgnoreCondition(property));
    }

    /// <summary>
    /// Reads the write-time condition a property's own <c>[Ignore]</c> annotation sets.
    /// </summary>
    /// <param name="property">The property symbol.</param>
    /// <returns>
    /// The numeric value of the annotation's <c>Condition</c>, or <see langword="null" /> when the property has no
    /// <c>[Ignore]</c> annotation; a mapped property never carries <c>IgnoreCondition.Always</c>.
    /// </returns>
    private static int? ReadIgnoreCondition(IPropertySymbol property)
    {
        foreach (AttributeData attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != IgnoreAttributeName)
                continue;

            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == "Condition" && argument.Value.Value is int condition)
                    return condition;
            }

            return IgnoreConditionAlways;
        }

        return null;
    }

    /// <summary>
    /// Resolves the scalar conversion strategy for a (nullable-unwrapped) property type.
    /// </summary>
    /// <param name="type">The effective property type.</param>
    /// <returns>The scalar kind, or <see cref="ScalarKind.Unsupported" />.</returns>
    private static ScalarKind ResolveScalarKind(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
            return ScalarKind.Enum;

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return ScalarKind.String;
            case SpecialType.System_Boolean:
                return ScalarKind.Boolean;
            case SpecialType.System_Char:
                return ScalarKind.Char;
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
                return ScalarKind.Number;
            case SpecialType.System_DateTime:
                return ScalarKind.DateTime;
            default:
                break;
        }

        // A reference type's nullable annotation is not part of its name.
        return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString() switch
        {
            "System.Guid" => ScalarKind.Guid,
            "System.DateTimeOffset" => ScalarKind.DateTimeOffset,
            "System.TimeSpan" => ScalarKind.TimeSpan,
            "System.DateOnly" => ScalarKind.DateOnly,
            "System.TimeOnly" => ScalarKind.TimeOnly,
            "System.Uri" => ScalarKind.Uri,
            "System.Int128" or "System.UInt128" or "System.Half" or "System.Numerics.BigInteger" => ScalarKind.Number,
            _ => ScalarKind.Unsupported,
        };
    }
}
