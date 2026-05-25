// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatDisplayName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Test.Kat;

/// <summary>
/// Produces scenario-named display strings for MSTest <c>[DynamicData]</c> rows whose first element is an
/// <see cref="IKat" /> instance, so that failures in CI and Test Explorer name the scenario rather than
/// showing an opaque row index.
/// </summary>
/// <remarks>
/// <para>
/// Wire this helper to a test method via the <c>DynamicDataDisplayName</c> argument of the
/// <c>DynamicData</c> attribute. The signature matches the MSTest contract
/// <c>static string (MethodInfo methodInfo, object?[] data)</c>.
/// </para>
/// <para>
/// When the first element of <paramref name="data" /> implements <see cref="IKat" />, the returned string
/// is <c>"{methodName}({kat.Name})"</c>; otherwise the method name is returned unchanged. This allows the
/// helper to be applied uniformly even on test methods whose rows are not all KATs.
/// </para>
/// </remarks>
public static class KatDisplayName
{
    /// <summary>
    /// Returns a display name for a single MSTest <c>[DynamicData]</c> row.
    /// </summary>
    /// <param name="methodInfo">The test method whose row is being named.</param>
    /// <param name="data">The row payload supplied by the dynamic data source.</param>
    /// <returns>
    /// <c>"{methodInfo.Name}({kat.Name})"</c> when the first element of <paramref name="data" /> implements
    /// <see cref="IKat" />; otherwise <paramref name="methodInfo" />.<see cref="MemberInfo.Name" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="methodInfo" /> is <see langword="null" />.</exception>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        if (data is { Length: > 0 } && data[0] is IKat kat)
            return $"{methodInfo.Name}({kat.Name})";

        return methodInfo.Name;
    }
}
