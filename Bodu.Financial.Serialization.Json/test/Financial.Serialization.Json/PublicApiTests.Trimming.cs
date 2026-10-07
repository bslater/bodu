// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiTests.Trimming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bodu.Test.Kat;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Verifies that every public entry point that creates or registers <see cref="MoneyOfTCurrencyJsonConverterFactory" />
/// declares <see cref="RequiresDynamicCodeAttribute" />: the factory closes
/// <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> over each currency type at run time, so a native AOT
/// application must be warned where the factory enters its serializer options.
/// </summary>
public partial class PublicApiTests
{
    /// <summary>
    /// Verifies that a public entry point that creates or registers
    /// <see cref="MoneyOfTCurrencyJsonConverterFactory" /> declares <see cref="RequiresDynamicCodeAttribute" />, so
    /// native AOT analysis warns where an application calls it.
    /// </summary>
    /// <param name="kat">The entry point, and whether it must declare the attribute.</param>
    [TestMethod]
    [DynamicData(nameof(DynamicCodeEntryPointCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EntryPoint_WhenCompiledAheadOfTime_ShouldRequireDynamicCode(BinaryKat<MethodBase, bool> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual(kat.Expected, kat.Input.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false));
    }

    /// <summary>
    /// Supplies one row for each public entry point that creates or registers
    /// <see cref="MoneyOfTCurrencyJsonConverterFactory" />: the factory's constructors, and every registration method
    /// of <see cref="FinancialJsonSerializerOptionsExtensions" /> and
    /// <see cref="FinancialJsonServiceCollectionExtensions" />.
    /// </summary>
    /// <returns>The entry-point rows, each labelled with the entry point's signature.</returns>
    public static IEnumerable<object[]> DynamicCodeEntryPointCases()
    {
        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        return typeof(MoneyOfTCurrencyJsonConverterFactory)
            .GetConstructors()
            .Concat<MethodBase>(typeof(FinancialJsonSerializerOptionsExtensions).GetMethods(PublicStatic))
            .Concat(typeof(FinancialJsonServiceCollectionExtensions).GetMethods(PublicStatic))
            .Select(entryPoint => new object[] { new BinaryKat<MethodBase, bool>(DescribeEntryPoint(entryPoint), entryPoint, true) });
    }

    /// <summary>
    /// Describes a constructor or method as its declaring type, its name, and its parameter types, for use as a row
    /// label.
    /// </summary>
    /// <param name="entryPoint">The constructor or method to describe.</param>
    /// <returns>A signature such as <c>MoneyOfTCurrencyJsonConverterFactory(FinancialJsonPolicy)</c>.</returns>
    private static string DescribeEntryPoint(MethodBase entryPoint)
    {
        string name = entryPoint.IsConstructor
            ? entryPoint.DeclaringType!.Name
            : $"{entryPoint.DeclaringType!.Name}.{entryPoint.Name}";

        return $"{name}({string.Join(", ", entryPoint.GetParameters().Select(parameter => parameter.ParameterType.Name))})";
    }
}
