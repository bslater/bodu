// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiTests.Trimming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bodu.Test.Kat;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that every public entry point that creates or registers a converter factory declares
/// <see cref="RequiresDynamicCodeAttribute" />: each factory closes its converter over the component type at run
/// time, so a native AOT application must be warned where a factory enters its serializer options. The closed
/// converters, which native AOT can compile, must not declare it.
/// </summary>
public partial class PublicApiTests
{
    /// <summary>
    /// Verifies that a public entry point declares <see cref="RequiresDynamicCodeAttribute" /> exactly when it creates
    /// or registers a converter factory, so native AOT analysis warns where an application calls it and stays quiet
    /// for the closed converters.
    /// </summary>
    /// <param name="kat">The entry point, and whether it must declare the attribute.</param>
    [TestMethod]
    [DynamicData(nameof(DynamicCodeEntryPointCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EntryPoint_WhenCompiledAheadOfTime_ShouldRequireDynamicCodeOnlyForFactories(BinaryKat<MethodBase, bool> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual(kat.Expected, kat.Input.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false));
    }

    /// <summary>
    /// Supplies one row for each public constructor of the converter factories and the closed converters, and for
    /// each public method of <see cref="NumericsJsonSerializerOptionsExtensions" /> and
    /// <see cref="FractionJsonExtensions" />, which register the factories.
    /// </summary>
    /// <returns>The entry-point rows, each labelled with the entry point's signature.</returns>
    public static IEnumerable<object[]> DynamicCodeEntryPointCases()
    {
        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        Type[] factories =
        [
            typeof(FractionJsonConverterFactory),
            typeof(IntervalJsonConverterFactory),
            typeof(DiscreteIntervalJsonConverterFactory),
            typeof(IntervalSetJsonConverterFactory),
            typeof(ComplexJsonConverterFactory),
        ];

        Type[] closedConverters =
        [
            typeof(FractionJsonConverter<int>),
            typeof(IntervalJsonConverter<double>),
            typeof(DiscreteIntervalJsonConverter<int>),
            typeof(IntervalSetJsonConverter<double>),
            typeof(ComplexJsonConverter<double>),
            typeof(BigDecimalJsonConverter),
        ];

        IEnumerable<MethodBase> requiring = factories
            .SelectMany(factory => factory.GetConstructors())
            .Concat<MethodBase>(typeof(NumericsJsonSerializerOptionsExtensions).GetMethods(PublicStatic))
            .Concat(typeof(FractionJsonExtensions).GetMethods(PublicStatic));

        IEnumerable<MethodBase> notRequiring = closedConverters.SelectMany(converter => converter.GetConstructors());

        return requiring
            .Select(entryPoint => Row(entryPoint, true))
            .Concat(notRequiring.Select(entryPoint => Row(entryPoint, false)));
    }

    /// <summary>
    /// Describes a constructor or method as its declaring type, its name, and its parameter types, for use as a row
    /// label.
    /// </summary>
    /// <param name="entryPoint">The constructor or method to describe.</param>
    /// <returns>A signature such as <c>FractionJsonConverterFactory(NumericsJsonPolicy)</c>.</returns>
    private static string DescribeEntryPoint(MethodBase entryPoint)
    {
        string name = entryPoint.IsConstructor
            ? entryPoint.DeclaringType!.Name
            : $"{entryPoint.DeclaringType!.Name}.{entryPoint.Name}";

        return $"{name}({string.Join(", ", entryPoint.GetParameters().Select(parameter => parameter.ParameterType.Name))})";
    }

    /// <summary>
    /// Builds one data row for an entry point.
    /// </summary>
    /// <param name="entryPoint">The constructor or method.</param>
    /// <param name="requiresDynamicCode">Whether the entry point must declare <see cref="RequiresDynamicCodeAttribute" />.</param>
    /// <returns>The data row.</returns>
    private static object[] Row(MethodBase entryPoint, bool requiresDynamicCode) =>
        [new BinaryKat<MethodBase, bool>(DescribeEntryPoint(entryPoint), entryPoint, requiresDynamicCode)];
}
