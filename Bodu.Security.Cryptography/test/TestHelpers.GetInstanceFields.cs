using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bodu
{
    public static partial class TestHelpers
    {
        /// <summary>
        /// Enumerates all instance fields in the algorithm and its base types to validate disposal state.
        /// </summary>
        /// <typeparam name="T">The type to reflect over.</typeparam>
        /// <param name="ignore">An optional array of field names to exclude from the results.</param>
        /// <param name="flags">The binding flags to use when enumerating fields.</param>
        /// <returns>An enumerable of object arrays, each containing a single <see cref="FieldInfo" />.</returns>
        public static IEnumerable<object[]> GetFieldInfoForType<T>(
            string[]? ignore = null,
            bool excludeReadOnly = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            ignore ??= Array.Empty<string>();
            Type type = typeof(T);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(flags))
                {
                    if (field.IsStatic || field.FieldType.IsEnum || !visited.Add(field.Name))
                        continue;
                    if (excludeReadOnly && field.IsInitOnly)
                        continue;
                    if (ignore.Contains(field.Name))
                        continue;

                    yield return new object[] { field };
                }

                type = type.BaseType;
            }
        }
    }
}