using System.Reflection;

namespace Bodu.Test;

public static partial class TestHelpers
{
    public enum PropertyAccessMode
    {
        Read,
        Write,
        ReadOrWrite,
    }

    /// <summary>
    /// Returns properties declared on the specified type and its base types that match the requested access mode.
    /// </summary>
    /// <typeparam name="T">
    /// The type whose properties are to be returned.
    /// </typeparam>
    /// <param name="accessMode">
    /// Specifies whether to return readable properties, writable properties, or both.
    /// </param>
    /// <param name="excludeInitOnly">
    /// <see langword="true" /> to exclude init-only properties when writable properties are requested.
    /// </param>
    /// <param name="flags">
    /// The binding flags used to select properties on each type in the inheritance chain.
    /// </param>
    /// <returns>
    /// A sequence of <c>object[]</c> values, each containing a single <see cref="PropertyInfo" />.
    /// If no matching properties are found, a single row containing <see langword="null" /> is returned.
    /// </returns>
    public static IEnumerable<object[]> GetPropertyInfoForType<T>(
        PropertyAccessMode accessMode = PropertyAccessMode.Write,
        bool excludeInitOnly = true,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly,
        params string[] excludeProperties)
    {
        Type? type = typeof(T);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;

        while (type is not null && type != typeof(object))
        {
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (!visited.Add(property.Name))
                {
                    continue;
                }

                bool canRead = property.GetMethod is not null;
                bool canWrite = property.SetMethod is not null;

                bool matches = accessMode switch
                {
                    PropertyAccessMode.Read => canRead,
                    PropertyAccessMode.Write => canWrite,
                    PropertyAccessMode.ReadOrWrite => canRead || canWrite,
                    _ => false,
                };

                if (!matches)
                {
                    continue;
                }

                if (excludeInitOnly &&
                    (accessMode == PropertyAccessMode.Write || accessMode == PropertyAccessMode.ReadOrWrite) &&
                    IsInitOnly(property))
                {
                    continue;
                }

                if (excludeProperties.Contains(property.Name))
                    continue;

                count++;
                yield return new object[] { property };
            }

            type = type.BaseType;
        }

        if (count == 0)
        {
            yield return new object[] { null! };
        }
    }

    private static bool IsInitOnly(PropertyInfo property)
    {
        MethodInfo? setMethod = property.SetMethod;
        if (setMethod is null)
        {
            return false;
        }

        return setMethod.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
    }
}