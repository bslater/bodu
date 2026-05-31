// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTestDataSource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides <see cref="DynamicDataAttribute" /> data-source and display-name helpers that enumerate
/// every concrete <see cref="SymmetricAlgorithm" /> type shipped by <c>Bodu.Security.Cryptography</c>.
/// </summary>
/// <remarks>
/// <para>
/// These methods must live in a non-generic static class so that MSTest's reflection-based method
/// lookup — which does not apply <see cref="System.Reflection.BindingFlags.FlattenHierarchy" /> —
/// can locate them directly by type reference rather than traversing a generic base class.
/// MSTest 4.x also requires <c>DynamicDataDisplayNameDeclaringType</c> to be set explicitly;
/// the constructor-supplied type is used only for the data-source method, not the display-name method.
/// </para>
/// <para>
/// Use with <c>[DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData),
/// typeof(SymmetricAlgorithmTestDataSource),
/// DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName),
/// DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]</c>.
/// </para>
/// </remarks>
internal static class SymmetricAlgorithmTestDataSource
{
    /// <summary>
    /// Provides one row per concrete <see cref="SymmetricAlgorithm" /> declared in the
    /// <c>Bodu.Security.Cryptography</c> assembly that exposes a public parameterless constructor.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is the <see cref="Type" /> of a
    /// <see cref="SymmetricAlgorithm" />-derived sealed class.
    /// </returns>
    public static IEnumerable<object[]> SymmetricAlgorithmTestData()
    {
        Assembly assembly = typeof(IPaddingStrategy).Assembly;
        foreach (Type type in assembly.GetTypes()
            .Where(IsConstructibleSymmetricAlgorithm)
            .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            yield return new object[] { type };
        }
    }

    /// <summary>
    /// Renders the <see cref="SymmetricAlgorithm" /> type name for a row produced by
    /// <see cref="SymmetricAlgorithmTestData" />, so MSTest's test explorer surfaces the per-cipher
    /// row name (e.g. <c>Skipjack</c>) instead of the default ordinal.
    /// </summary>
    /// <param name="methodInfo">The test method (unused; required by <see cref="DynamicDataAttribute" />).</param>
    /// <param name="data">The single-element row produced by <see cref="SymmetricAlgorithmTestData" />.</param>
    /// <returns>The simple type name of the row's <see cref="Type" /> argument.</returns>
    public static string GetSymmetricAlgorithmDisplayName(MethodInfo methodInfo, object[] data) =>
        ((Type)data[0]).Name;

    /// <summary>
    /// Filters a candidate <see cref="Type" /> down to the concrete, parameterless,
    /// <see cref="SymmetricAlgorithm" />-derived classes shipped by <c>Bodu.Security.Cryptography</c>.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="type" /> is a non-abstract, non-generic class that
    /// derives from <see cref="SymmetricAlgorithm" /> and exposes a public parameterless constructor; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Stream ciphers such as <see cref="ChaCha20" /> are excluded implicitly: they derive from the native
    /// <see cref="SymmetricStreamAlgorithm" /> rather than <see cref="SymmetricAlgorithm" />, so the padding- and
    /// mode-conformance suites driven by this data source — which have no meaning for an additive stream cipher — never
    /// see them.
    /// </remarks>
    private static bool IsConstructibleSymmetricAlgorithm(Type type) =>
        type.IsClass
        && !type.IsAbstract
        && !type.IsGenericTypeDefinition
        && typeof(SymmetricAlgorithm).IsAssignableFrom(type)
        && type.GetConstructor(Type.EmptyTypes) is not null;
}
