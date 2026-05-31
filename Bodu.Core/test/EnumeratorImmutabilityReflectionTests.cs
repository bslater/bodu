// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumeratorImmutabilityReflectionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Reflection;

namespace Bodu;

[TestClass]
public sealed class EnumeratorImmutabilityReflectionTests
{

    private static readonly string[] s_requiredReadOnlyProperties =
    [
        nameof(IEnumerator.Current)
    ];

    /// <summary>
    /// Provides all value types in the current assembly that implement IEnumerator or IEnumerator&lt;T&gt;.
    /// </summary>
    public static IEnumerable<object[]> GetEnumeratorStructTypes()
    {
        Assembly assembly = typeof(Bodu.ThrowHelper).Assembly;

        foreach (Type type in assembly.GetTypes())
        {
            if (!type.IsValueType || type.IsEnum)
                continue;

            if (typeof(IEnumerator).IsAssignableFrom(type) ||
                type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerator<>)))
            {
                yield return new object[] { type };
            }
        }
    }

    /// <summary>
    /// Verifies that all required properties from IEnumerator or IEnumerator&lt;T&gt; are read-only (getter only).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetEnumeratorStructTypes))]
    public void EnumeratorInterfaceProperties_ShouldBeReadOnly(Type enumeratorType)
    {
        PropertyInfo[] props = enumeratorType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var violatingProps = props
            .Where(p => s_requiredReadOnlyProperties.Contains(p.Name) && p.CanWrite)
            .Select(p => p.Name)
            .ToList();

        if (violatingProps.Count > 0)
        {
            Assert.Fail($"Type '{enumeratorType.FullName}' has mutable interface properties: {string.Join(", ", violatingProps)}");
        }
    }

}
