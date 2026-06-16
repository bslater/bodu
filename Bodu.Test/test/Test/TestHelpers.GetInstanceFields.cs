using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bodu.Test;

public static partial class TestHelpers
{
    /// <summary>
    /// Enumerates instance fields declared by <typeparamref name="T" /> and its base types for disposal-state validation.
    /// </summary>
    /// <typeparam name="T">
    /// The type to reflect over.
    /// </typeparam>
    /// <param name="excludeReadOnly">
    /// <see langword="true" /> to exclude read-only fields; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="excludePropertyBackingFields">
    /// <see langword="true" /> to exclude compiler-generated auto-property backing fields; otherwise,
    /// <see langword="false" />.
    /// </param>
    /// <param name="flags">
    /// The binding flags used when enumerating fields. <see cref="BindingFlags.DeclaredOnly" /> is added internally
    /// while walking each type in the inheritance chain.
    /// </param>
    /// <param name="excludeFields">
    /// An optional set of field names to exclude from the results.
    /// </param>
    /// <returns>
    /// An enumerable of object arrays suitable for dynamic test data, where each item contains a single
    /// <see cref="FieldInfo" /> instance. If no fields are found, a single item containing <see langword="null" />
    /// is returned.
    /// </returns>
    /// <remarks>
    /// Static fields and enum-typed fields are always excluded. Base types are processed explicitly so that inherited
    /// fields can be considered while avoiding duplicate field names from hidden members.
    /// </remarks>
    public static IEnumerable<object[]> GetFieldInfoForType<T>(
        bool excludeReadOnly = false,
        bool excludePropertyBackingFields = true,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        params string[] excludeFields)
    {
        Type? type = typeof(T);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var excluded = new HashSet<string>(excludeFields ?? [], StringComparer.Ordinal);

        int count = 0;

        while (type != null && type != typeof(object))
        {
            BindingFlags declaredOnlyFlags = flags | BindingFlags.DeclaredOnly;

            foreach (FieldInfo field in type.GetFields(declaredOnlyFlags))
            {
                if (field.IsStatic || field.FieldType.IsEnum || !visited.Add(field.Name))
                    continue;

                if (excludeReadOnly && field.IsInitOnly)
                    continue;

                if (excludePropertyBackingFields && IsAutoPropertyBackingField(field))
                    continue;

                if (excluded.Contains(field.Name))
                    continue;

                count++;
                yield return new object[] { field };
            }

            type = type.BaseType;
        }

        if (count == 0)
            yield return new object[] { null! };
    }

    /// <summary>
    /// Determines whether the specified field is a compiler-generated backing field for an auto-property.
    /// </summary>
    /// <param name="field">
    /// The field to inspect.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="field" /> appears to be a compiler-generated auto-property
    /// backing field; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Auto-property backing fields are emitted by the compiler using names such as
    /// <c>&lt;PropertyName&gt;k__BackingField</c> and are marked with
    /// <see cref="CompilerGeneratedAttribute" />. Manually implemented property backing fields cannot be
    /// detected reliably using this check.
    /// </remarks>
    private static bool IsAutoPropertyBackingField(FieldInfo field) =>
        field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
        field.Name.StartsWith('<') &&
        field.Name.Contains(">k__BackingField", StringComparison.Ordinal);
}