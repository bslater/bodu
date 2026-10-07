// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Trimming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that every public <see cref="BencodeSerializer" /> entry point that reflects over the type it maps declares
/// <see cref="RequiresUnreferencedCodeAttribute" /> and <see cref="RequiresDynamicCodeAttribute" />, so an application
/// that trims or compiles ahead of time is warned where it calls the serializer.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a public serializer entry point that reflects over the type it maps declares
    /// <see cref="RequiresUnreferencedCodeAttribute" />, so trimming analysis warns where an application calls it.
    /// </summary>
    /// <param name="kat">The entry point, and whether it must declare the attribute.</param>
    [TestMethod]
    [DynamicData(nameof(ReflectingEntryPointCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EntryPoint_WhenTrimmed_ShouldRequireUnreferencedCode(BinaryKat<MethodBase, bool> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual(kat.Expected, kat.Input.IsDefined(typeof(RequiresUnreferencedCodeAttribute), inherit: false));
    }

    /// <summary>
    /// Verifies that a public serializer entry point that reflects over the type it maps declares
    /// <see cref="RequiresDynamicCodeAttribute" />, so native AOT analysis warns where an application calls it.
    /// </summary>
    /// <param name="kat">The entry point, and whether it must declare the attribute.</param>
    [TestMethod]
    [DynamicData(nameof(ReflectingEntryPointCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EntryPoint_WhenCompiledAheadOfTime_ShouldRequireDynamicCode(BinaryKat<MethodBase, bool> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual(kat.Expected, kat.Input.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false));
    }

    /// <summary>
    /// Supplies one row for each public <see cref="BencodeSerializer" /> entry point that reflects over the type it
    /// maps: every generic method, and every method that receives the type as a <see cref="Type" /> argument.
    /// </summary>
    /// <returns>The entry-point rows, each labelled with the entry point's signature.</returns>
    public static IEnumerable<object[]> ReflectingEntryPointCases() =>
        typeof(BencodeSerializer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsGenericMethodDefinition || method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Type)))
            .Select(method => new object[] { new BinaryKat<MethodBase, bool>(DescribeEntryPoint(method), method, true) });

    /// <summary>
    /// Describes a method as its name, its type parameters, and its parameter types, for use as a row label.
    /// </summary>
    /// <param name="method">The method to describe.</param>
    /// <returns>A signature such as <c>Deserialize&lt;T&gt;(Stream, BencodeSerializerOptions)</c>.</returns>
    private static string DescribeEntryPoint(MethodInfo method)
    {
        string typeParameters = method.IsGenericMethodDefinition
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(DescribeParameterType))}>"
            : string.Empty;
        string parameters = string.Join(", ", method.GetParameters().Select(parameter => DescribeParameterType(parameter.ParameterType)));

        return $"{method.Name}{typeParameters}({parameters})";
    }

    /// <summary>
    /// Describes a parameter type by its simple name, writing a constructed generic type with its type arguments.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>A name such as <c>IBufferWriter&lt;Byte&gt;</c>.</returns>
    private static string DescribeParameterType(Type type) =>
        type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", type.GetGenericArguments().Select(DescribeParameterType))}>"
            : type.Name;
}
